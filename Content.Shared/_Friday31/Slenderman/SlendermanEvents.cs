using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Friday31.Slenderman;

public sealed partial class SlendermanDismemberActionEvent : EntityTargetActionEvent
{
    [DataField]
    public SoundSpecifier? DismemberSound;

    [DataField]
    public float SoundRange = 10f;
}

[Serializable, NetSerializable]
public sealed class SlendermanScreamerEvent : EntityEventArgs
{
    public float Duration;

    public SlendermanScreamerEvent(float duration)
    {
        Duration = duration;
    }
}
