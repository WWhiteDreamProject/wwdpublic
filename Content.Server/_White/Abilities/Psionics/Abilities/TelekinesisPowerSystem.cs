using Content.Shared._White.Abilities.Psionics;
using Content.Shared._White.Actions.Events;
using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server._White.Abilities.Psionics.Abilities;

public sealed class TelekinesisPowerSystem : SharedTelekinesisPowerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelekinesisPowerComponent, GetVerbsEvent<InnateVerb>>(AddTelekinesisVerb);
    }

    private void AddTelekinesisVerb(EntityUid uid, TelekinesisPowerComponent component, GetVerbsEvent<InnateVerb> args)
    {
        var victim = args.Target;

        if (!TryComp<PhysicsComponent>(victim, out var physics))
            return;

        if (TryComp<MobStateComponent>(victim, out var mobState))
            return;

        if (component.TetheredEntity == null)
        {
            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartTether(uid, component, victim, args.User, physics);
                },
                Text = Loc.GetString("telekinesis-verb-tether"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Nyanotrasen/Icons/verbiconfangs.png")),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
        else
        {
            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StopTether(uid, component);
                },
                Text = Loc.GetString("telekinesis-verb-untether"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Nyanotrasen/Icons/verbiconfangs.png")),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        //UpdateTethers(frameTime);
    }

    protected override void StartTether(EntityUid userUid, TelekinesisPowerComponent component, EntityUid target, EntityUid user,
        PhysicsComponent? targetPhysics = null, TransformComponent? targetXform = null)
    {
        base.StartTether(userUid, component, target, user, targetPhysics, targetXform);

        var powerEv = new TelekinesisPowerActionEvent { Target = target };
        RaiseLocalEvent(userUid, powerEv);
    }

    protected override void StopTether(EntityUid uid, TelekinesisPowerComponent component, bool land = true, bool transfer = false) => base.StopTether(uid, component);
}
