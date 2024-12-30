using Content.Server.Aliens.Components;
using Content.Server.Weapons.Melee;
using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;


namespace Content.Server.Aliens.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AlienAcidSystem : EntitySystem
{
    /// <inheritdoc/>
    [Dependency] private readonly MeleeWeaponSystem _meleeWeapon = default!;
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
            if (_tag.HasTag(hitEntity, "Wall"))
            {
                var acid = Spawn(component.AcidPrototype, hitEntity.ToCoordinates());
                var acidComp = EnsureComp<AlienAcidComponent>(acid);
                acidComp.MeltTimeSpan = _timing.CurTime + TimeSpan.FromSeconds(component.MeltTime);
                acidComp.WallUid = hitEntity;
            }
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<AlienAcidComponent>();
        var deletingQueue = new List<EntityUid>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.WallUid == null || _timing.CurTime < component.MeltTimeSpan)
                continue;
            deletingQueue.Add(uid);
            deletingQueue.Add(component.WallUid!.Value);
        }
        foreach (var uid in deletingQueue)
            QueueDel(uid);
    }
}
