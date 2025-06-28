namespace Content.Shared._White.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class ClonePowerComponent : Component { 
        [ViewVariables] public EntityUid? CloneUid;
    }
    [RegisterComponent]
    public sealed partial class PsionicCloneComponent : Component { 
        [ViewVariables] public EntityUid? OriginalUid;
    }
}
