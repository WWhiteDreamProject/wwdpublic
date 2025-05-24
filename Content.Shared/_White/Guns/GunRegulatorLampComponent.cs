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
    [DataField("safeTemp", required: true), AutoNetworkedField]
    public float SafeTemperatureCelcius { get => SafeTemperature - 273.15f; set => SafeTemperature = value + 273.15f; }
    public float SafeTemperature = 0; // unit test bs
    /// <summary>
    /// Temperature at or above which the lamp is guaranteed to break immediately after shooting
    /// Increased by 273.15f upon component init.
    /// </summary>
    [DataField("unsafeTemp", required: true), AutoNetworkedField]
    public float UnsafeTemperatureCelcius { get => UnsafeTemperature - 273.15f; set => UnsafeTemperature = value + 273.15f; }
    public float UnsafeTemperature = 1; // unit test bs

    [DataField, AutoNetworkedField]
    public bool Intact = true;

    [DataField]
    public SoundSpecifier BreakSound = new SoundCollectionSpecifier("GlassBreak");
}
