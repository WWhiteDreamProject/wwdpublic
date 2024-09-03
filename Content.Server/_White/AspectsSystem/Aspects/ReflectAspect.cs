using Content.Server.GameTicking.Rules.Components;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server._White.Other;
using Content.Server.GameTicking.Components;
using Content.Shared.Weapons.Reflect;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class ReflectAspect : AspectSystem<ReflectAspectComponent>
{
    protected override void Started(EntityUid uid, ReflectAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var query = EntityQueryEnumerator<ReflectAspectMarkComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            var reflect = EnsureComp<ReflectComponent>(ent);
            reflect.ReflectProb = 1;
            reflect.Reflects = ReflectType.Energy | ReflectType.NonEnergy;
        }
    }

}
