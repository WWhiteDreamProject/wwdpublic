using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Components;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Preferences;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Appearance.Systems;

public abstract partial class SharedBodyAppearanceSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    protected EntityQuery<BodyAppearanceProviderComponent> ProviderQuery;

    private EntityQuery<HumanoidProfileComponent> _humanoidProfileQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyAppearanceComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<BodyAppearanceComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedBodySystem)]);

        InitializeProvider();

        ProviderQuery = GetEntityQuery<BodyAppearanceProviderComponent>();

        _humanoidProfileQuery = GetEntityQuery<HumanoidProfileComponent>();
    }

    #region Event Handling

    private void OnGetVerbs(Entity<BodyAppearanceComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_admin.HasAdminFlag(args.User, AdminFlags.Fun))
            return;

        var user = args.User;
        args.Verbs.Add(
            new Verb
        {
            Text = Loc.GetString("body-appearance-modify-markings-verb"),
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Customization/reptilian_parts.rsi"), "tail_smooth"),
            Act = () =>
            {
                _userInterface.OpenUi(ent.Owner, MarkingModifierKey.Key, user);
            },
        });
    }

    private void OnMapInit(Entity<BodyAppearanceComponent> ent, ref MapInitEvent args)
    {
        if (!_humanoidProfileQuery.TryComp(ent, out var humanoidProfileComp))
            return;

        var profile = new HumanoidCharacterProfile();
        profile.WithSpecies(humanoidProfileComp.Species);
        profile.WithBodyType(humanoidProfileComp.BodyType);
        profile.WithSex(humanoidProfileComp.Sex);
        ApplyProfile(ent.AsNullable(), profile);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gathers the body appearance data from this entity.
    /// </summary>
    /// <param name="uid">The entity to sample.</param>
    /// <param name="data">Returned provider specifier body appearance data.</param>
    public bool TryGetData(EntityUid uid, out Dictionary<BodyProviderType, BodyAppearanceData> data)
    {
        var ev = new GetBodyAppearanceDataEvent();
        RaiseLocalEvent(uid, ref ev);

        data = ev.Data;

        return data.Count > 0;
    }

    /// <summary>
    /// Applies profile data to all appearance providers within the body.
    /// </summary>
    /// <param name="uid">The body to apply the provider appearance to.</param>
    /// <param name="data">The appearance to apply.</param>
    public void ApplyAppearanceData(EntityUid uid, BodyAppearanceData data)
    {
        var profileEvt = new ApplyBodyAppearanceDataEvent(data, null);
        RaiseLocalEvent(uid, ref profileEvt);
    }

    /// <summary>
    /// Applies profile data to the specified appearance provider within the body.
    /// </summary>
    /// <param name="uid">The body to apply the provider appearance to.</param>
    /// <param name="data">The appearance to apply.</param>
    public void ApplyAppearanceData(EntityUid uid, Dictionary<BodyProviderType, BodyAppearanceData> data)
    {
        var profileEvt = new ApplyBodyAppearanceDataEvent(null, data);
        RaiseLocalEvent(uid, ref profileEvt);
    }

    /// <summary>
    /// Applies the information contained with a <see cref="HumanoidCharacterProfile"/> to a body's appearance.
    /// </summary>
    /// <param name="uid">The body to apply the profile to</param>
    /// <param name="profile">The profile to apply</param>
    public void ApplyProfile(EntityUid uid, HumanoidCharacterProfile profile)
    {
        var appearanceData = new BodyAppearanceData
        {
            BodyColoration = new (profile.BodyColoration),
            BodyType = profile.BodyType,
            Sex = profile.Sex,
        };

        ApplyAppearanceData(uid, appearanceData);
    }

    #endregion
}

[Serializable, NetSerializable]
public enum MarkingModifierKey
{
    Key,
}

/// <summary>
/// Event raised on body entity when profiles are being applied to it
/// </summary>
[ByRefEvent]
public readonly record struct ApplyBodyAppearanceDataEvent(BodyAppearanceData? Data, Dictionary<BodyProviderType, BodyAppearanceData>? SpecifiedData);

/// <summary>
/// Event raised on a body entity, when its appearance is being copied from a body provider.
/// </summary>
/// <param name="Provider">The entity whose appearance is being copied.</param>
[ByRefEvent]
public readonly record struct CopyBodyAppearanceEvent(Entity<BodyProviderComponent> Provider);

/// <summary>
/// Event raised on an entity to get the body appearance on its provider.
/// </summary>
[ByRefEvent]
public readonly record struct GetBodyAppearanceDataEvent()
{
    /// <summary>
    /// A result contained the appearance data.
    /// </summary>
    public readonly Dictionary<BodyProviderType, BodyAppearanceData> Data = new();
}

[Serializable, NetSerializable]
public sealed class MarkingModifierMarkingSetMessage(
    Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markings)
    : BoundUserInterfaceMessage
{
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings { get; } = markings;
}

[Serializable, NetSerializable]
public sealed class MarkingModifierState(
    Dictionary<BodyProviderType, BodyAppearanceData> appearanceData,
    Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markings,
    Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> markingsData)
    : BoundUserInterfaceState
{
    public Dictionary<BodyProviderType, BodyAppearanceData> AppearanceData { get; } = appearanceData;
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings { get; } = markings;
    public Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> MarkingsData { get; } = markingsData;
}
