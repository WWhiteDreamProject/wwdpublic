using System.Linq;
using Content.Shared._White.Bark.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;


namespace Content.Shared._White.Bark.Systems;


public abstract class SharedBarkSystem : EntitySystem
{
    private static readonly char[] LongPauseChars = ['.', ',', '?', '!',];
    private static readonly char[] SkipChars = [' ', '\n', '\r', '\t',];
    private static readonly char[] Soglasnoy = ['Б', 'В', 'Г', 'Д', 'Ж', 'З', 'Й', 'К', 'Л', 'М', 'Н', 'П', 'Р', 'С', 'Т', 'Ф', 'Х', 'Ц', 'Ч', 'Ш', 'Щ', ];

    public void Bark(Entity<BarkComponent> entity, string text, bool isWhisper)
    {
        var barkList = new List<BarkData>();

        foreach (var currChar in text)
        {
            var currBark = new BarkData(entity.Comp.PitchAverage, entity.Comp.VolumeAverage, entity.Comp.PauseAverage);

            if (SkipChars.Contains(currChar))
                currBark.Enabled = false;

            if (LongPauseChars.Contains(currChar))
            {
                currBark.Pause += 0.5f;
                currBark.Enabled = false;
            }

            if (isWhisper)
            {
                currBark.Volume -= SharedAudioSystem.GainToVolume(4f);
            }

            //if (char.IsUpper(currChar))
            //    currBark.Pitch += 0.4f;

            if (Soglasnoy.Contains(currChar))
            {
                currBark.Pitch -= 0.2f;
                currBark.Volume -= SharedAudioSystem.GainToVolume(4f);
                currBark.Pause *= 0.8f;
            }

            currBark.Pitch += System.Random.Shared.NextFloat(-entity.Comp.PitchVariance, entity.Comp.PitchVariance);

            barkList.Add(currBark);
        }

        Bark(entity, barkList);
    }

    public abstract void Bark(Entity<BarkComponent> entity, List<BarkData> barks);

}

[Serializable, NetSerializable]
public sealed class EntityBarkEvent(NetEntity entity, List<BarkData> barks) : EntityEventArgs
{
    public NetEntity Entity { get; } = entity;
    public List<BarkData> Barks { get; } = barks;
}

[Serializable, NetSerializable]
public enum CharacterVoiceType
{
    None,
    Bark,
    TTS,
}
