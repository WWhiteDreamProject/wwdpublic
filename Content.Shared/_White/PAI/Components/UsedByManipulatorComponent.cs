namespace Content.Shared._White.PAI.Components;

[RegisterComponent]
public sealed partial class UsedByManipulatorComponent : Component
{
    [DataField]
    public EntityUid ManipulatorOwner;
}
