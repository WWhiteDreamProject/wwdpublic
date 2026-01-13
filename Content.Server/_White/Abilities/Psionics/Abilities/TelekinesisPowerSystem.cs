using Content.Shared._White.Abilities.Psionics;
using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
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
        if (!HasComp<PsionicComponent>(uid))
            return;

        var victim = args.Target;

        if (!TryComp<PhysicsComponent>(victim, out var physics))
            return;

        if (HasComp<MobStateComponent>(victim)) //no mob telekinesis
            return;

        if (component.TetheredEntity == null || victim != component.TetheredEntity)
        {
            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartTether(uid, component, victim, physics);
                },
                Text = Loc.GetString("telekinesis-verb-tether"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/_White/Interface/VerbIcons/telekinesis.png")),
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
                Icon = new SpriteSpecifier.Texture(new("/Textures/_White/Interface/VerbIcons/telekinesis.png")),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }

    protected override void StartTether(EntityUid userUid, TelekinesisPowerComponent component, EntityUid target,
        PhysicsComponent? targetPhysics = null, TransformComponent? targetXform = null) =>
        base.StartTether(userUid, component, target, targetPhysics, targetXform);

    protected override void StopTether(EntityUid uid, TelekinesisPowerComponent component) =>
        base.StopTether(uid, component);
}
