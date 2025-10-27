using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared._Friday31.Jason;

public sealed partial class DecapitateActionEvent : EntityTargetActionEvent
{
    [DataField]
    public SoundSpecifier? Sound;
}
