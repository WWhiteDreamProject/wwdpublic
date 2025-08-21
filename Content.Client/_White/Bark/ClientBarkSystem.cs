using Content.Shared._White.Bark;
using Content.Shared._White.Bark.Components;
using Content.Shared._White.Bark.Systems;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._White.Bark;

public sealed class BarkSystem : SharedBarkSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AudioSystem _sharedAudio = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeNetworkEvent<EntityBarkEvent>(OnEntityBark);
    }

    private void OnEntityBark(EntityBarkEvent ev)
    {
        var ent = GetEntity(ev.Entity);
        if(!TryComp<BarkComponent>(ent, out var comp))
            return;

        Bark(new(ent, comp), ev.Barks);
    }


    public override void Bark(Entity<BarkComponent> entity, List<BarkData> barks)
    {
        if (TryComp<BarkSourceComponent>(entity, out var sourceComponent))
        {
            RemComp(entity, sourceComponent);
        }

        sourceComponent = AddComp<BarkSourceComponent>(entity);
        sourceComponent.Barks = new(barks);
        sourceComponent.ResolvedSound = _sharedAudio.ResolveSound(entity.Comp.BarkSound);
    }

    public void Bark(EntityUid entity, ResolvedSoundSpecifier soundSpecifier, BarkData currentBark)
    {
        if(!currentBark.Enabled)
            return;

        _sharedAudio
            .PlayEntity(
                soundSpecifier,
                Filter.Local(),
                entity,
                true,
                new AudioParams()
                    .WithPitchScale(currentBark.Pitch)
                    .WithVolume(currentBark.Volume));
    }

    public override void Update(float frameTime)
    {
        if(!_gameTiming.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<BarkSourceComponent>();
        while (query.MoveNext(out var uid, out var barkSource))
        {
            barkSource.BarkTime += frameTime;

            if (barkSource.CurrentBark is null)
            {
                if (!barkSource.Barks.TryDequeue(out var barkData))
                {
                    RemComp(uid, barkSource);
                    continue;
                }

                barkSource.CurrentBark = barkData;
            }

            if (barkSource.CurrentBark.Value.Pause <= barkSource.BarkTime)
            {
                barkSource.BarkTime = 0;
                Bark(uid, barkSource.ResolvedSound, barkSource.CurrentBark.Value);
                barkSource.CurrentBark = null;
            }
        }
    }
}
