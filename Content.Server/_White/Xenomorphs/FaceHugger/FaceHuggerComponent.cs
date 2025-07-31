using Content.Shared.EntityEffects;

namespace Content.Server._White.Xenomorphs.FaceHugger;

[RegisterComponent]
public sealed partial class FaceHuggerComponent : Component
{
    [DataField]
    public List<EntityEffect> Effects = new ();
}
