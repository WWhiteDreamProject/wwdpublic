using Content.Shared._White.PAI.Components;
using Content.Shared._White.PAI.Events;
using Content.Shared.Item;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;

namespace Content.Client._White.PAI;

public sealed class ManipulatorSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        _input.FirstChanceOnKeyEvent += OnKey;
    }

    private void OnKey(KeyEventArgs args, KeyEventType type)
    {
        if (args.Handled)
            return;

        if (type != KeyEventType.Down)
            return;

        var local = _player.LocalEntity;

        if (!TryComp<ManipulatorComponent>(local, out var comp) ||
            !comp.IsActive ||
            comp.IsReturning ||
            comp.Manipulator == null)
            return;

        if (args.Key == Keyboard.Key.MouseLeft)
        {
            var clicked = _input.MouseScreenPosition;
            var pos = _eye.ScreenToMap(clicked);

            var moveEv = new ManipulatorMoveEvent(pos);
            RaiseNetworkEvent(moveEv);
        }

        if (args.Key == Keyboard.Key.E)
        {
            var ev = new ManipulatorInteractEvent();
            RaiseNetworkEvent(ev);
        }

        if (args.Key == Keyboard.Key.Q)
        {
            var pos = _transform.GetMapCoordinates(Transform(comp.Manipulator.Value));
            var entitiesUnderneath = _lookup.GetEntitiesInRange(pos, 0.1f);

            foreach (var ent in entitiesUnderneath)
            {
                if (ent == local || ent == comp.Manipulator)
                    continue;

                if (!EntityManager.EntityExists(ent))
                    continue;

                if (!HasComp<ItemComponent>(ent))
                    continue;

                if (!TryComp<PhysicsComponent>(ent, out var phys))
                    continue;

                if (phys.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (_container.IsEntityInContainer(ent))
                    continue;

                var grabEv = new ManipulatorGrabEvent(GetNetEntity(ent));
                RaiseNetworkEvent(grabEv);
                break;
            }
        }
    }
}
