using Content.Shared.Chat;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Shared._White.BloodCult.Runes.Components;

[RegisterComponent]
public sealed partial class BloodCultRuneComponent : Component
{
    [DataField]
    public string? InvokePhrase;

    [DataField]
    public InGameICChatType InvokeChatType = InGameICChatType.Whisper;

    [DataField]
    public int RequiredInvokers = 1;

    [DataField]
    public float RuneActivationRange = 1f;

    /// <summary>
    ///     Damage dealt on the rune activation.
    /// </summary>
    [DataField]
    public DamageSpecifier? ActivationDamage;

    /// <summary>
    ///     Will the rune upon activation set nearest Rending Rune Placement Marker to disabled.
    /// </summary>
    [DataField]
    public bool TriggerRendingMarkers;

    [DataField]
    public bool CanBeErased = true;

    [DataField]
    public EntityWhitelist? Whitelist = new() {Components = ["BloodCultist", ], };

    public ProtoId<ReagentPrototype> HolyWaterPrototype = "HolyWater";
}

[Serializable, NetSerializable]
public enum BloodRuneVisuals
{
    Active,
    Layer
}

public sealed class InvokeRuneEvent(EntityUid user, HashSet<EntityUid> invokers) : CancellableEntityEventArgs
{
    public EntityUid User = user;
    public HashSet<EntityUid> Invokers = invokers;
}

public sealed class AfterInvokeRuneEvent(EntityUid user) : EntityEventArgs
{
    public EntityUid User = user;
}

public sealed class AfterRunePlaced(EntityUid user)
{
    public EntityUid User = user;
}
