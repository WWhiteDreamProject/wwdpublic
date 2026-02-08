using Robust.Shared.Serialization;

namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public enum PdaVisuals
    {
        IdCardInserted,
        State // WD EDIT
    }

    [Serializable, NetSerializable]
    public enum PdaUiKey
    {
        Key
    }

    // WD EDIT START
    [Serializable, NetSerializable]
    public enum PdaState : byte
    {
        Closed,
        Closing,
        Open,
        Opening
    }

    public enum PdaVisualLayers : byte
    {
        Flashlight,
        IdLight,
        Screen,
        State
    }
    // WD EDIT END
}
