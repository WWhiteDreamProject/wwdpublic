using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Guns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RegulatorLampComponent : Component
{
    /// <summary>
    /// Temperature below which the lamp is guaranteed to work
    /// Increased by 273.15f upon component init.
    /// </summary>
    [DataField("safeTemp", required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SafeTemperatureCelcius { get => SafeTemperature - 273.15f; set => SafeTemperature = value + 273.15f; }
    public float SafeTemperature;
    /// <summary>
    /// Temperature at or above which the lamp is guaranteed to break immediately after shooting
    /// Increased by 273.15f upon component init.
    /// </summary>
    [DataField("unsafeTemp", required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float UnsafeTemperatureCelcius { get => UnsafeTemperature - 273.15f; set => UnsafeTemperature = value + 273.15f; }
    public float UnsafeTemperature;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Intact = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BreakSound = new SoundCollectionSpecifier("GlassBreak");
}
