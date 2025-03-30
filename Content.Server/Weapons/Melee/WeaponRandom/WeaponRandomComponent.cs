using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Weapons.Melee.WeaponRandom;

[RegisterComponent]
internal sealed partial class WeaponRandomComponent : Component
{

    /// <summary>
    /// Amount of damage that will be caused. This is specified in the yaml.
    /// </summary>
    [DataField("damageBonus")]
    public DamageSpecifier DamageBonus = new();

    /// <summary>
    /// Chance for the damage bonus to occur (1 = 100%).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)] //WWDP EDIT
    public float RandomDamageChance = 0.00001f;

    /// <summary>
    /// Sound effect to play when the damage bonus occurs.
    /// </summary>
    [DataField("damageSound")]
    public SoundSpecifier DamageSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    //WWDP EDIT START
    /// <summary>
    /// Apply throwing bonus instead of melee
    /// </summary>
    [DataField]
    public bool ApplyBonusOnThrow;

    /// <summary>
    /// Forces all successful throws to target this specific body part.
    /// </summary>
    [DataField]
    public TargetBodyPart? ForcedThrowTargetPart;

    /// <summary>
    /// Temporary flag indicating that a critical throw is in progress
    /// </summary>
    /// /// <remarks>
    /// Set during ThrowDoHitEvent if critical chance check passes,
    /// then consumed in GetThrowingDamageEvent to add bonus damage.
    /// </remarks>
    [DataField]
    public bool IsCriticalThrow;
    //WWDP EDIT END
}
