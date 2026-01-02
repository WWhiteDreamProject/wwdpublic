using Robust.Shared.Serialization;

namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public enum PdaVisuals
    {
        IdCardInserted,
        PdaType,
        Enabled, // WWDP edit
        Screen // WWDP edit
    }

    [Serializable, NetSerializable]
    public enum PdaUiKey
    {
        Key
    }

}
