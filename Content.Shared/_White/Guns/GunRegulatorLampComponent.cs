using Content.Shared.Decals;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._White.Guns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FluxCoreComponent : Component
{
    /// <summary>
    /// Total flux capacity.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Capacity;

    /// <summary>
    /// Weapons will not fire if the flux levels would increase above this value.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float SafeFlux;

    /// <summary>
    /// At flux levels below this, the weapon will have <see cref="BaseMalfunctionChance"/> chance to experience a malfunction.
    /// For flux values between base and max, linear interpolation is used to determine malfunction chance.
    /// Null defaults to zero.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? BaseMalfunctionChanceFlux;

    /// <summary>
    /// At flux levels below this, the weapon will have <see cref="MaxMalfunctionChance"/> chance to experience a malfunction.
    /// For flux values between base and max, linear interpolation is used to determine malfunction chance.
    /// Null defaults to <see cref="Capacity"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? MaxMalfunctionChanceFlux;


    [DataField, AutoNetworkedField]
    public float BaseMalfunctionChance;

    [DataField, AutoNetworkedField]
    public float MaxMalfunctionChance;

    /// At flux levels below this, shot projectiles will have their damage multiplied by <see cref="BaseDamageIncrease"/>.
    /// For flux values between base and max, linear interpolation is used to determine damage multiplier.
    /// Null defaults to zero.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? BaseDamageMultiplierFlux;

    /// <summary>
    /// At flux levels below this, shot projectiles will have their damage multiplied by <see cref="MaxDamageIncrease"/>.
    /// For flux values between base and max, linear interpolation is used to determine damage multiplier.
    /// Null defaults to <see cref="Capacity"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? MaxDamageMultiplierFlux;

    [DataField, AutoNetworkedField]
    public float BaseDamageMultiplier = 1;
     
    [DataField, AutoNetworkedField]
    public float MaxDamageMultiplier = 1;


    [DataField]
    public TimeSpan DecayTimeToStart = TimeSpan.Zero;

    [DataField(required: true), AutoNetworkedField]
    public float DecayRate;

    [DataField, AutoNetworkedField]
    public float DecayCurve = 1f;

    /// <summary>
    /// use <see cref="SharedGunFluxSystem.GetCurrentFlux(FluxCoreComponent)"/> and <see cref="SharedGunFluxSystem.AddFlux(FluxCoreComponent, float)"/>
    /// </summary>
    [Access(typeof(SharedGunFluxSystem), Other = AccessPermissions.None)]
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentFlux;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan LastFluxUpdate;

    [DataField]
    public int EffectSteps;

    [DataField]
    public string EffectState = "flux";

    [DataField]
    public float OverflowDamageMultiplier = 1.0f;

    [DataField]
    public float UnderflowDamageMultiplier = 1.0f;
}
