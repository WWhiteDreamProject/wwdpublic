using Content.Shared._White.Body.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

// WD EDITED
[Serializable, NetSerializable]
public sealed partial class HealingDoAfterEvent : DoAfterEvent
{
    [DataField]
    public BodyPartType BodyPartType;

    public HealingDoAfterEvent(BodyPartType bodyPartType)
    {
        BodyPartType = bodyPartType;
    }

    public override DoAfterEvent Clone() => this;
}
