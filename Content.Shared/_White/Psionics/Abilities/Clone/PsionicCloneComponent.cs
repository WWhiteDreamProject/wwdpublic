namespace Content.Shared._White.Abilities.Psionics;

[RegisterComponent]
public sealed partial class PsionicCloneComponent : Component {
    [ViewVariables]
    public EntityUid? OriginalUid;
}