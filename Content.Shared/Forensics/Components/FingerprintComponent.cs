using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component is for mobs that leave fingerprints.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FingerprintComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Fingerprint;

    /// <summary>
    /// WWDP
    /// We still might want to leave marks without fingerprints e.g. gloves.
    /// Or disable fingerprints temporarily
    /// </summary>
    [DataField]
    public bool NotLeavingFingerprints;
}
