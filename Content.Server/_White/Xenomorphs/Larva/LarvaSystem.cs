using Content.Server.Body.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Server.Containers;

namespace Content.Server._White.Xenomorphs.Larva;

public sealed class LarvaSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LarvaComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
    }

    private void OnTakeGhostRole(EntityUid uid, LarvaComponent component, TakeGhostRoleEvent args)
    {
        if (!_container.TryGetContainingContainer((uid, null, null), out var container))
            return;

        if (TryComp<BodyComponent>(container.Owner, out var body))
            _body.GibBody(container.Owner, body: body);
        else if (TryComp<BodyPartComponent>(container.Owner, out var bodyPart) && bodyPart.Body.HasValue)
            _body.GibBody(bodyPart.Body.Value);
        else
            _container.Remove(uid, container);
    }
}
