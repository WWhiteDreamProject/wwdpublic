using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Systems;

namespace Content.Shared._White.Appearance.Systems;

public abstract partial class SharedBodyAppearanceSystem : EntitySystem
{
    [Dependency] private readonly MarkingManager _marking = default!;

    protected EntityQuery<BodyAppearanceComponent> BodyQuery;
    protected EntityQuery<BodyAppearanceProviderComponent> ProviderQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyAppearanceComponent, ApplyProfileEvent>(OnApplyProfile);

        InitializeProvider();

        BodyQuery = GetEntityQuery<BodyAppearanceComponent>();
        ProviderQuery = GetEntityQuery<BodyAppearanceProviderComponent>();
    }

    #region Event Handling

    private void OnApplyProfile(Entity<BodyAppearanceComponent> ent, ref ApplyProfileEvent args)
    {
        var data = new BodyAppearanceData
        {
            Colors = new (args.Profile.Colors),
            BodyType = args.Profile.BodyType,
            Sex = args.Profile.Sex,
        };

        ApplyAppearanceData(ent, data);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gathers the body appearance data from this entity.
    /// </summary>
    /// <param name="uid">The entity to sample.</param>
    /// <param name="data">Returned provider specifier body appearance data.</param>
    /// <returns>True if the appearance data is successfully returned, false otherwise.</returns>
    public bool TryGetAppearance(EntityUid uid, out Dictionary<Enum, BodyAppearanceData> data)
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
        var ev = new ApplyBodyAppearanceDataEvent(data, null);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Applies profile data to the specified appearance provider within the body.
    /// </summary>
    /// <param name="uid">The body to apply the provider appearance to.</param>
    /// <param name="data">The appearance to apply.</param>
    public void ApplyAppearanceData(EntityUid uid, Dictionary<Enum, BodyAppearanceData> data)
    {
        var ev = new ApplyBodyAppearanceDataEvent(null, data);
        RaiseLocalEvent(uid, ref ev);
    }

    #endregion
}

/// <summary>
/// Event raised on body entity when profiles are being applied to it
/// </summary>
/// <param name="Data">The appearance data to apply to all providers.</param>
/// <param name="SpecifiedData">The appearance data to apply to specific providers.</param>
[ByRefEvent]
public readonly record struct ApplyBodyAppearanceDataEvent(BodyAppearanceData? Data, Dictionary<Enum, BodyAppearanceData>? SpecifiedData) : IBodyRelayEvent
{
    /// <summary>
    /// The body provider to which we apply appearance data.
    /// </summary>
    public BodyProviderType ProviderType { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised on an entity to get the body appearance on its provider.
/// </summary>
[ByRefEvent]
public readonly record struct GetBodyAppearanceDataEvent() : IBodyRelayEvent
{
    /// <summary>
    /// The body provider whose appearance data we wont get.
    /// </summary>
    public BodyProviderType ProviderType { get; } = BodyProviderType.All;

    /// <summary>
    /// A result contained the appearance data.
    /// </summary>
    public readonly Dictionary<Enum, BodyAppearanceData> Data = new();
}

/// <summary>
/// Event raised on an entity to get his body appearance layers.
/// </summary>
[ByRefEvent]
public readonly record struct GetBodyAppearanceLayersEvent()
{
    /// <summary>
    /// A result contained the appearance layers.
    /// </summary>
    public readonly HashSet<Enum> Layers = new();
}
