using Content.Shared.Mobs;

namespace Content.Shared._White.BloodCult.Construct;

public sealed class ConstructSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConstructComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ConstructComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<ConstructComponent> construct, ref MapInitEvent args)
    {
        _appearance.SetData(construct, ConstructVisualsState.Transforming, true);
        construct.Comp.Transforming = true;
    }

    private void OnMobStateChanged(EntityUid uid, ConstructComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var xform = Transform(uid);
        Spawn(component.SpawnOnDeathPrototype, xform.Coordinates);

        QueueDel(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ConstructComponent>();
        while (query.MoveNext(out var uid, out var construct))
        {
            if (!construct.Transforming)
                continue;

            construct.TransformAccumulator += frameTime;
            if (construct.TransformAccumulator < construct.TransformDelay)
                continue;

            construct.TransformAccumulator = 0f;
            construct.Transforming = false;
            _appearance.SetData(uid, ConstructVisualsState.Transforming, false);
        }
    }
}
