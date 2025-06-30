using Robust.Shared.Audio;

namespace Content.Shared._White.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class ClonePowerComponent : Component { 
        [ViewVariables] public EntityUid? CloneUid;
        [DataField]
        public SoundSpecifier CloneSound = new SoundPathSpecifier("/Audio/_White/Object/Devices/experimentalsyndicateteleport.ogg");
        [DataField]
        public string? CloneEffect = "ExperimentalTeleporterInEffect";
    }
    [RegisterComponent]
    public sealed partial class PsionicCloneComponent : Component { 
        [ViewVariables] public EntityUid? OriginalUid;
    }
    
}
