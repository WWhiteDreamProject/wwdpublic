using Content.Shared.DoAfter;

namespace Content.Shared._White.Medical.Wounds.Components;

[RegisterComponent]
public sealed partial class ExaminableWoundableComponent : Component
{

}

public sealed partial class ExamineWoundableDoAfterEvent : SimpleDoAfterEvent;
