using Content.Shared.Mind;
using Content.Shared.Roles;


namespace Content.Shared._White.Roles;


public sealed class RolesCacheSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MindComponent, RoleAddingEvent>(OnRoleAdded);
        SubscribeLocalEvent<RoleCacheComponent, RoleRemovingEvent>(OnRoleRemoved);
    }

    private void OnRoleRemoved(EntityUid uid, RoleCacheComponent component, RoleRemovingEvent args)
    {
        if (args.RoleComponent.Antag)
            component.AntagWeight -= 1;
    }

    private void OnRoleAdded(EntityUid uid, MindComponent component, RoleAddingEvent args)
    {
        var cacheComp = EnsureComp<RoleCacheComponent>(uid);

        if (args.RoleComponent.Antag)
            cacheComp.AntagWeight += 1;

        if(args.RoleComponent.JobPrototype != null)
            cacheComp.JobPrototype = args.RoleComponent.JobPrototype;
        if(args.RoleComponent.AntagPrototype != null)
            cacheComp.AntagPrototype = args.RoleComponent.AntagPrototype;
    }
}
