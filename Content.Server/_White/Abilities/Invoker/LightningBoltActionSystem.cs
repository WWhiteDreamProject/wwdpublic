using Content.Server.Lightning;
using Content.Shared._White.Actions.Events;
using Content.Shared.Mind.Components;
using System.Linq;

namespace Content.Server._White.Abilities.Invoker;

public sealed class LightningBoltActionSystem : EntitySystem
{
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningBoltActionEvent>(OnUsed);
    }

    private void OnUsed(LightningBoltActionEvent args)
    {
        var entities = _lookup.GetEntitiesInRange(Transform(args.Performer).Coordinates, 7f)
            .Where(e => e != args.Performer && HasComp<MindContainerComponent>(e))
            .Take(3)
            .ToList();

        if (entities.Count == 0)
            return;

        foreach (var target in entities)
        {
            _lightning.ShootLightning(args.Performer, target);
        }

        args.Handled = true;
    }
}
