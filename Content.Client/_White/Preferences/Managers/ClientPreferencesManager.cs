using System.Linq;
using Content.Shared._White.Preferences;
using Content.Shared.Preferences;
using Robust.Client;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client._White.Preferences.Managers;

public sealed partial class ClientPreferencesManager : IClientPreferencesManager
{
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IClientNetManager _net = default!;

    public event Action? OnServerDataLoaded;

    public GameSettings Settings { get; private set; } = default!;
    public PlayerPreferences Preferences { get; private set; } = default!;

    public void Initialize()
    {
        _net.RegisterNetMessage<PreferencesAndSettingsResponseMessage>(OnPreferencesAndSettingsResponse);

        _net.RegisterNetMessage<DeleteCharacterRequestMessage>();
        _net.RegisterNetMessage<SelectCharacterRequestMessage>();
        _net.RegisterNetMessage<UpdateCharacterRequestMessage>();

        _baseClient.RunLevelChanged += OnRunLevelChanged;
    }

    #region Event Handling

    private void OnPreferencesAndSettingsResponse(PreferencesAndSettingsResponseMessage msg)
    {
        Preferences = msg.Preferences;
        Settings = msg.Settings;

        OnServerDataLoaded?.Invoke();
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel != ClientRunLevel.Initialize)
            return;

        Settings = default!;
        Preferences = default!;
    }

    #endregion

    #region Public API

    public void CreateCharacter(HumanoidCharacterProfile profile)
    {
        var characters = new Dictionary<int, HumanoidCharacterProfile>(Preferences.Characters);
        var lowest = Enumerable.Range(0, Settings.MaxCharacterSlots)
            .Except(characters.Keys)
            .FirstOrNull();

        if (lowest == null)
            throw new InvalidOperationException("Out of character slots!");

        characters.Add(lowest.Value, profile);

        Preferences = new (characters, Preferences.SelectedCharacterIndex, Preferences.AdminOOCColor, Preferences.CustomGhost);

        UpdateCharacter(profile, lowest.Value);
    }

    public void DeleteCharacter(HumanoidCharacterProfile profile)
    {
        DeleteCharacter(Preferences.IndexOfCharacter(profile));
    }

    public void DeleteCharacter(int slot)
    {
        var characters = Preferences.Characters.Where(p => p.Key != slot);
        Preferences = new (characters, Preferences.SelectedCharacterIndex, Preferences.AdminOOCColor, Preferences.CustomGhost);
        var msg = new DeleteCharacterRequestMessage { Slot = slot, };
        _net.ClientSendMessage(msg);
    }

    public void SelectCharacter(HumanoidCharacterProfile profile)
    {
        SelectCharacter(Preferences.IndexOfCharacter(profile));
    }

    public void SelectCharacter(int slot)
    {
        Preferences = new (Preferences.Characters, slot, Preferences.AdminOOCColor, Preferences.CustomGhost);
        var msg = new SelectCharacterRequestMessage { SelectedCharacterIndex = slot, };
        _net.ClientSendMessage(msg);
    }

    public void UpdateCharacter(HumanoidCharacterProfile profile, int slot)
    {
        profile.EnsureValid();
        var characters = new Dictionary<int, HumanoidCharacterProfile>(Preferences.Characters) {[slot] = profile};
        Preferences = new (characters, Preferences.SelectedCharacterIndex, Preferences.AdminOOCColor, Preferences.CustomGhost);

        var msg = new UpdateCharacterRequestMessage
        {
            Profile = profile,
            Slot = slot,
        };
        _net.ClientSendMessage(msg);
    }

    #endregion
}
