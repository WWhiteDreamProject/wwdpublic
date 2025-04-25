namespace Content.Server.Forensics
{
    /// <summary>
    /// This component is for mobs that leave fingerprints.
    /// </summary>
    [RegisterComponent]
    public sealed partial class FingerprintComponent : Component
    {
        [DataField("fingerprint"), ViewVariables(VVAccess.ReadWrite)]
        public string? Fingerprint;

        /// <summary>
        /// WWDP
        /// We still might want to leave marks without fingerprints e.g. gloves.
        /// Or disable fingerprints temporarily
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool NotLeavingFingerprints;
    }
}
