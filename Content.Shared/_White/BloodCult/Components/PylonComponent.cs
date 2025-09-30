using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.BloodCult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PylonComponent : Component
{
    [DataField]
    public bool IsActive = true;

    [DataField]
    public float HealingDelay = 20;

    [DataField]
    public float HealingAuraRange = 5;

    [DataField]
    public float CorruptionRadius = 5;

    /// <summary>
    ///     Length of the cooldown in between tile corruptions.
    /// </summary>
    [DataField]
    public float CorruptionCooldown = 5;

    /// <summary>
    ///     Length of the cooldown in between healinng.
    /// </summary>
    [DataField]
    public float HealingCooldown = 20;

    [DataField]
    public string CultTile = "BloodCultFloor";

    [DataField]
    public EntProtoId TileCorruptEffect = "BloodCultTileSpawnEffect";

    [DataField]
    public SoundSpecifier CorruptTileSound = new SoundPathSpecifier("/Audio/_White/Magic/BloodCult/curse.ogg");

    [DataField]
    public DamageSpecifier Healing = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public float CorruptionAccumulator = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public float HealingAccumulator = 0;
}
