using Content.Server.Body.Components;
using Content.Server.Ghost.Components;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Pointing;

namespace Content.Server.Body.Systems
{
    public sealed class BrainSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, OrganAddedEvent>((uid, _, args) => HandleMind(args.Body, uid)); // WD EDIT
            SubscribeLocalEvent<BrainComponent, OrganRemovedEvent>((uid, _, args) => HandleMind(uid, args.Body)); // WD EDIT
            SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
        }

        private void HandleMind(EntityUid? newEntity, EntityUid? oldEntity) // WD EDIT
        {
            if (!newEntity.HasValue || !oldEntity.HasValue || TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity)) // WD EDIT
                return;

            EnsureComp<MindContainerComponent>(newEntity.Value); // WD EDIT
            EnsureComp<MindContainerComponent>(oldEntity.Value); // WD EDIT

            var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity.Value); // WD EDIT
            if (HasComp<BodyComponent>(newEntity))
                ghostOnMove.MustBeDead = true;

            if (!_mindSystem.TryGetMind(oldEntity.Value, out var mindId, out var mind)) // WD EDIT
                return;

            _mindSystem.TransferTo(mindId, newEntity, mind: mind);
        }

        private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
        {
            args.Cancel();
        }
    }
}
