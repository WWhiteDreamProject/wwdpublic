using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._White.Serialization;
using Content.Server.Database;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared._White.Preferences;
using Content.Shared.CCVar;
using Content.Shared.Clothing.Loadouts.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server._White.Preferences.Managers;

/// <summary>
/// Manages player preferences and character data on the server.
/// </summary>
public sealed partial class ServerPreferencesManager : IServerPreferencesManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly UserDbDataManager _userDbData = default!;

    private ISawmill _sawmill = default!;

    private readonly Dictionary<NetUserId, PlayerPreferencesData> _playersPreferencesData = new();

    private int _maxCharacterSlots;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("preferences");

        _net.RegisterNetMessage<DeleteCharacterRequestMessage>(OnDeleteCharacterRequest);
        _net.RegisterNetMessage<SelectCharacterRequestMessage>(OnSelectCharacterRequest);
        _net.RegisterNetMessage<UpdateCharacterRequestMessage>(OnUpdateCharacterRequest);

        _net.RegisterNetMessage<PreferencesAndSettingsResponseMessage>();

        _configuration.OnValueChanged(CCVars.GameMaxCharacterSlots, value => _maxCharacterSlots = value, true);
    }

    void IPostInjectInit.PostInject()
    {
        _userDbData.AddOnLoadPlayer(LoadData);
        _userDbData.AddOnFinishLoad(FinishLoad);
        _userDbData.AddOnPlayerDisconnect(OnClientDisconnected);
    }

    #region Event Handling

    private async void OnDeleteCharacterRequest(DeleteCharacterRequestMessage msg)
    {
        if (!_playersPreferencesData.TryGetValue(msg.MsgChannel.UserId, out var preferencesData) || !preferencesData.PreferencesLoaded)
        {
            _sawmill.Warning($"User {msg.MsgChannel.UserId} tried to modify preferences before they loaded.");
            return;
        }

        if (msg.Slot < 0 || msg.Slot >= _maxCharacterSlots)
            return;

        if (preferencesData.Preferences is not {} preferences)
            return;

        var nextSlot = preferences.SelectedCharacterIndex;
        if (preferences.SelectedCharacterIndex == msg.Slot)
        {
            (nextSlot, var profile) = preferences.Characters.FirstOrDefault(p => p.Key != msg.Slot);
            if (profile == null)
                return;
        }

        var characters = new Dictionary<int, HumanoidCharacterProfile>(preferences.Characters);
        characters.Remove(msg.Slot);

        preferencesData.Preferences = new (characters, nextSlot, preferences.AdminOOCColor, preferences.CustomGhost);

        if (!ShouldStorePreference(msg.MsgChannel.AuthType))
            return;

        if (nextSlot == preferences.SelectedCharacterIndex)
        {
            await _db.DeleteSlotAndSetSelectedIndex(msg.MsgChannel.UserId, msg.Slot, nextSlot);
            return;
        }

        await _db.SaveCharacterSlotAsync(msg.MsgChannel.UserId, null, msg.Slot);
    }

    private async void OnSelectCharacterRequest(SelectCharacterRequestMessage msg)
    {
        if (!_playersPreferencesData.TryGetValue(msg.MsgChannel.UserId, out var preferencesData) || !preferencesData.PreferencesLoaded)
        {
            _sawmill.Warning($"User {msg.MsgChannel.UserId} tried to modify preferences before they loaded.");
            return;
        }

        if (msg.SelectedCharacterIndex < 0 || msg.SelectedCharacterIndex >= _maxCharacterSlots)
            return;

        if (preferencesData.Preferences is not {} preferences)
            return;

        if (!preferences.Characters.ContainsKey(msg.SelectedCharacterIndex))
            return;

        preferencesData.Preferences = new PlayerPreferences(preferences.Characters, msg.SelectedCharacterIndex, preferences.AdminOOCColor, preferences.CustomGhost);

        if (!ShouldStorePreference(msg.MsgChannel.AuthType))
            return;

        await _db.SaveSelectedCharacterIndexAsync(msg.MsgChannel.UserId, msg.SelectedCharacterIndex);
    }

    private async void OnUpdateCharacterRequest(UpdateCharacterRequestMessage msg)
    {
        await SetProfile(msg.MsgChannel.UserId, msg.Slot, msg.Profile);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Converts a legacy profile from the database into a charter profile.
    /// </summary>
    /// <param name="profile">The profile from the database.</param>
    /// <returns>A converted <see cref="HumanoidCharacterProfile"/>.</returns>
    internal HumanoidCharacterProfile ConvertProfiles(Profile profile)
    {
        var jobs = profile.Jobs.ToDictionary(j => new ProtoId<JobPrototype>(j.JobName), j => (JobPriority) j.Priority);
        var antags = profile.Antags.Select(a => new ProtoId<AntagPrototype>(a.AntagName));
        var traits = profile.Traits.Select(t => new ProtoId<TraitPrototype>(t.TraitName));
        var loadouts = profile.Loadouts.Select(l =>
            new Loadout(
                l.LoadoutName,
                l.CustomName,
                l.CustomDescription,
                l.CustomContent,
                l.CustomColorTint,
                l.CustomHeirloom
                ));
        var bodyColoration = profile.BodyColoration.ToDictionary(x => new ProtoId<BodyColorationPrototype>(x.Coloration), x => Color.FromHex(x.Color));

        var sex = Sex.Male;
        if (Enum.TryParse<Sex>(profile.Sex, true, out var sexVal))
            sex = sexVal;

        var gender = sex == Sex.Male ? Gender.Male : Gender.Female;
        if (Enum.TryParse<Gender>(profile.Gender, true, out var genderVal))
            gender = genderVal;

        var markings = new Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>>();
        if (profile.Markings?.RootElement is { } markingsElement)
        {
            var data = markingsElement.ToDataNode();
            markings = _serialization
                .Read<Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>>>(
                    data,
                    notNullableOverride: true);
        }

        var bodyProviders = new Dictionary<string, EntProtoId?>();
        if (profile.BodyProviders?.RootElement is { } bodyProvidersElement)
        {
            var data = bodyProvidersElement.ToDataNode();
            bodyProviders = _serialization.Read<Dictionary<string, EntProtoId?>>(data, notNullableOverride: true);
        }

        return new HumanoidCharacterProfile(
            jobs,
            bodyColoration,
            markings,
            bodyProviders,
            loadouts.ToDictionary(p => p.LoadoutName),
            antags.ToHashSet(),
            traits.ToHashSet(),
            new()
            {
                Pause = profile.BarkPause,
                Pitch = profile.BarkPitch,
                PitchVariance = profile.BarkPitchVariance,
                Volume = profile.BarkVolume,
            },
            profile.Height,
            profile.Width,
            gender,
            profile.Age,
            (PreferenceUnavailableMode) profile.PreferenceUnavailable,
            profile.Bark,
            profile.BodyType,
            profile.Employer,
            profile.Lifepath,
            profile.Nationality,
            profile.Species,
            profile.Voice,
            sex,
            (SpawnPriority) profile.SpawnPriority,
            profile.Flavor,
            profile.CharacterName);
    }

    /// <summary>
    /// Converts a preference object from the database into a player preferences.
    /// </summary>
    /// <param name="preference">The preference from the database.</param>
    /// <returns>A converted <see cref="PlayerPreferences"/>.</returns>
    internal PlayerPreferences ConvertPreferences(Preference preference)
    {
        var maxSlot = preference.Profiles.Max(p => p.Slot) + 1;

        var characters = new Dictionary<int, HumanoidCharacterProfile>(maxSlot);
        foreach (var profile in preference.Profiles)
        {
            characters[profile.Slot] = ConvertProfiles(profile);
        }

        return new PlayerPreferences(characters, preference.SelectedCharacterSlot, Color.FromHex(preference.AdminOOCColor), preference.GhostId);
    }

    /// <summary>
    /// Determines if preferences should be stored.
    /// </summary>
    /// <param name="loginType">The login type of the player's session.</param>
    /// <returns>True if preferences should be stored, false otherwise.</returns>
    internal static bool ShouldStorePreference(LoginType loginType)
    {
        return loginType.HasStaticUserId();
    }

    /// <summary>
    /// Loads player preferences for a connected session.
    /// </summary>
    /// <param name="session">The player session for whom to load preferences.</param>
    /// <param name="cancel">A cancellation token for the asynchronous operation.</param>
    public async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        if (!ShouldStorePreference(session.Channel.AuthType))
        {
            var preferences = new PlayerPreferences(
                new[] { new KeyValuePair<int, HumanoidCharacterProfile>(0, HumanoidCharacterProfile.Random()) },
                0,
                Color.Transparent,
                "default");

            _playersPreferencesData[session.UserId] = new() {PreferencesLoaded = true, Preferences = preferences };
            return;
        }

        var data = new PlayerPreferencesData();
        _playersPreferencesData[session.UserId] = data;

        await LoadPrefs();

        async Task LoadPrefs()
        {
            var preferences = await GetOrCreatePreferencesAsync(session.UserId, cancel);
            data.Preferences = ConvertPreferences(preferences);
        }
    }

    /// <summary>
    /// Sets or updates a character profile for a given user and slot.
    /// </summary>
    /// <param name="user">The network user ID of the player.</param>
    /// <param name="slot">The slot index of the character profile to update.</param>
    /// <param name="profile">The new or updated character profile data.</param>
    public async Task SetProfile(NetUserId user, int slot, HumanoidCharacterProfile profile)
    {
        if (!_playersPreferencesData.TryGetValue(user, out var preferencesData) || !preferencesData.PreferencesLoaded)
        {
            _sawmill.Error($"Tried to modify user {user} preferences before they loaded.");
            return;
        }

        if (slot < 0 || slot >= _maxCharacterSlots)
            return;

        if (preferencesData.Preferences is not {} preferences)
            return;

        profile.EnsureValid();

        var characters = new Dictionary<int, HumanoidCharacterProfile>(preferences.Characters);
        preferencesData.Preferences = new (characters, slot, preferences.AdminOOCColor, preferences.CustomGhost);

        var session = _player.GetSessionById(user);
        if (!ShouldStorePreference(session.Channel.AuthType))
            return;

        await _db.SaveCharacterSlotAsync(user, profile, slot);
    }

    /// <summary>
    /// Checks if a player has loaded preferences.
    /// </summary>
    /// <param name="session">The player session to check.</param>
    /// <returns>True if preferences are loaded for the player, false otherwise.</returns>
    public bool HavePreferences(ICommonSession session)
    {
        return _playersPreferencesData.ContainsKey(session.UserId);
    }

    /// <summary>
    /// Tries to retrieve the preferences for a given user from the cache.
    /// </summary>
    /// <param name="user">The network user ID of the player.</param>
    /// <param name="preferences">Output parameter: The user's preferences if found and loaded; otherwise, null.</param>
    /// <returns>True if preferences were found and loaded, false otherwise.</returns>
    public bool TryGetPreferences(NetUserId user, [NotNullWhen(true)] out PlayerPreferences? preferences)
    {
        if (_playersPreferencesData.TryGetValue(user, out var preferencesData))
        {
            preferences = preferencesData.Preferences;
            return preferences != null;
        }

        preferences = null;
        return false;
    }

    /// <summary>
    /// Retrieves the selected character profile for a list of specified players.
    /// </summary>
    /// <param name="users">A list of network user IDs to get selected profiles for.</param>
    /// <returns>An enumerable of key-value pairs, where the key is the user ID and the value is their selected character profile.</returns>
    public IEnumerable<KeyValuePair<NetUserId, HumanoidCharacterProfile>> GetSelectedProfilesForPlayers(List<NetUserId> users)
    {
        foreach (var user in users)
        {
            if (!_playersPreferencesData.TryGetValue(user, out var preferencesData))
                continue;

            if (preferencesData.Preferences == null)
                continue;

            yield return new (user, preferencesData.Preferences.SelectedCharacter);
        }
    }

    /// <summary>
    /// Retrieves the preferences for a given user from storage.
    /// </summary>
    /// <param name="user">The network user ID of the player.</param>
    /// <returns>The loaded <see cref="PlayerPreferences"/>.</returns>
    public PlayerPreferences GetPreferences(NetUserId user)
    {
        var preferences = _playersPreferencesData[user].Preferences;
        if (preferences == null)
            throw new InvalidOperationException("Preferences for this player have not loaded yet.");

        return preferences;
    }

    /// <summary>
    /// Retrieves preferences for the given username from storage, or returns null if the user is null or preferences are not loaded.
    /// </summary>
    /// <param name="user">The network user ID of the player.</param>
    /// <returns>The loaded <see cref="PlayerPreferences"/>, or null if not available.</returns>
    public PlayerPreferences? GetPreferencesOrNull(NetUserId? user)
    {
        if (user == null)
            return null;

        if (_playersPreferencesData.TryGetValue(user.Value, out var preferencesData))
            return preferencesData.Preferences;

        return null;
    }

    /// <summary>
    /// Called after player data has finished loading from the database.
    /// </summary>
    /// <param name="session">The player session whose data has finished loading.</param>
    public void FinishLoad(ICommonSession session)
    {
        var data = _playersPreferencesData[session.UserId];
        DebugTools.Assert(data.Preferences != null);
        data.Preferences = SanitizePreferences(data.Preferences);

        data.PreferencesLoaded = true;

        var msg = new PreferencesAndSettingsResponseMessage
        {
            Preferences = data.Preferences,
            Settings = new () { MaxCharacterSlots = _maxCharacterSlots, },
        };
        _net.ServerSendMessage(msg, session.Channel);
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    /// <param name="session">The player session that has disconnected.</param>
    public void OnClientDisconnected(ICommonSession session)
    {
        _playersPreferencesData.Remove(session.UserId);
    }

    #endregion

    #region Private API

    private async Task<Preference> GetOrCreatePreferencesAsync(NetUserId user, CancellationToken cancel)
    {
        var preferences = await _db.GetPlayerPreferencesAsync(user, cancel);
        if (preferences is null)
            return await _db.InitPrefsAsync(user, HumanoidCharacterProfile.Random(), cancel);

        return preferences;
    }

    private PlayerPreferences SanitizePreferences(PlayerPreferences prefs)
    {
        return new(
            prefs.Characters.Select(p => new KeyValuePair<int, HumanoidCharacterProfile>(p.Key, p.Value.Validated())),
            prefs.SelectedCharacterIndex,
            prefs.AdminOOCColor,
            prefs.CustomGhost);
    }

    #endregion
}
