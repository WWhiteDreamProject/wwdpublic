using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;


namespace Content.Shared._White.Bark;


public sealed partial class SoundBarkAction : IBarkAction
{
    [DataField] public ResolvedSoundSpecifier SoundSpecifier;
    public void Act(IDependencyCollection dependencyCollection, Entity<Components.BarkSourceComponent> entity, BarkData currentBark)
    {
        if(!currentBark.Enabled)
            return;

        dependencyCollection.Resolve<SharedAudioSystem>()
            .PlayEntity(
                SoundSpecifier,
                Filter.Broadcast(),
                entity,
                true,
                new AudioParams()
                    .WithPitchScale(currentBark.Pitch)
                    .WithVolume(currentBark.Volume));
    }
}
