using Content.Shared._White.Abilities.Psionics;
using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Components;

namespace Content.Server._White.Abilities.Psionics.Abilities;

public sealed class BarokinesisPowerSystem : SharedBarokinesisPowerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarokinesisPowerComponent, GetVerbsEvent<InnateVerb>>(AddBarokinesisVerb);
    }

    private void AddBarokinesisVerb(EntityUid uid, BarokinesisPowerComponent component, GetVerbsEvent<InnateVerb> args)
    {
        var force = component.BaseForce;

        if (TryComp<PsionicComponent>(uid, out var psionic))
            force = MathF.Pow(psionic.CurrentAmplification, 1.5f) + psionic.CurrentAmplification;

        else if (component.CheckPsionics)
            return;

        var victim = args.Target;

        if (!TryComp<PhysicsComponent>(victim, out var physics))
            return;

        var throwSpeed = component.ThrowSpeed;

        VerbCategory barokinesisCategory = new("barokinesis-verb");

        if (!HasComp<MobStateComponent>(victim))
        {
            InnateVerb pullVerb = new()
            {
                Act = () =>
                {
                    base.Pull(victim, uid, force, throwSpeed, physics, component);
                },
                Text = Loc.GetString("verb-barokinesis-pull"),
                Priority = 1,
                Category = barokinesisCategory
            };

            InnateVerb pushVerb = new()
            {
                Act = () =>
                {
                    base.Push(victim, uid, force, throwSpeed, physics, component);
                },
                Text = Loc.GetString("verb-barokinesis-push"),
                Priority = 2,
                Category = barokinesisCategory
            };
            args.Verbs.Add(pullVerb);
            args.Verbs.Add(pushVerb);
        }
        else if (args.User == args.Target)
        {
            InnateVerb dashVerb = new()
            {
                Act = () =>
                {
                    base.Dash(uid, force, throwSpeed, component);
                },
                Text = Loc.GetString("verb-barokinesis-dash"),
                Priority = 1
            };
            args.Verbs.Add(dashVerb);
        }
    }
}
