namespace Content.Shared._White.Wizard.Projectiles;

public sealed class EntityTrailSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityTrailComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<EntityTrailComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent, out TrailComponent? trailComponent))
            return;

        trailComponent.RenderedEntity = ent;
        Dirty(ent, trailComponent);
    }
}
