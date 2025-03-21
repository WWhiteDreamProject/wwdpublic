using Content.Server._White.Xenomorphs.Components;
using Content.Shared.Coordinates;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class AlienAcidSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AlienAcidComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, AlienAcidComponent component, MeleeHitEvent args)
    {
        foreach (var hitEntity in args.HitEntities)
        {
            if (!_tag.HasTag(hitEntity, "Wall"))
                continue;

            var acid = Spawn(component.AcidPrototype, hitEntity.ToCoordinates());
            var acidComp = EnsureComp<AlienAcidComponent>(acid);
            acidComp.MeltTimeSpan = _timing.CurTime + TimeSpan.FromSeconds(component.MeltTime);
            acidComp.WallUid = hitEntity;
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<AlienAcidComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.WallUid == null || _timing.CurTime < component.MeltTimeSpan)
                continue;

            QueueDel(uid);
            QueueDel(component.WallUid!.Value);
        }
    }
}
