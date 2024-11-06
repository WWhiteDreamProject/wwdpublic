using System.Numerics;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Telescope;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class ImmersiveAspect : AspectSystem<ImmersiveAspectComponent>
{
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly SharedTelescopeSystem _telescope = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    protected override void Started(EntityUid uid,
        ImmersiveAspectComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        OnStarted(component);
    }

    private void OnStarted(ImmersiveAspectComponent component)
    {
        var humans = EntityQuery<HumanoidAppearanceComponent>();

        foreach (var human in humans)
        {
            var entity = human.Owner;

            if (!HasComp<ContentEyeComponent>(entity))
                continue;

            SetEyeZoom(entity, component.EyeModifier);
            AddTelescope(entity, component.TelescopeDivisor, component.TelescopeLerpAmount);
        }
    }

    private void SetEyeZoom(EntityUid human, float modifier)
    {
        _eye.SetMaxZoom(human, new Vector2(modifier));
        _eye.SetZoom(human, new Vector2(modifier));
    }

    private void AddTelescope(EntityUid human, float divisor, float lerpAmount)
    {
        var telescope = EnsureComp<TelescopeComponent>(human);

        _telescope.SetParameters((human, telescope), divisor, lerpAmount);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!HasComp<ContentEyeComponent>(ev.Mob))
            return;

        var query = EntityQueryEnumerator<ImmersiveAspectComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleEntity, out var immersiveAspect, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEntity, gameRule))
                continue;

            SetEyeZoom(ev.Mob, immersiveAspect.EyeModifier);
            AddTelescope(ev.Mob, immersiveAspect.TelescopeDivisor, immersiveAspect.TelescopeLerpAmount);
        }
    }


    protected override void Ended(EntityUid uid,
        ImmersiveAspectComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        var humans = EntityQuery<HumanoidAppearanceComponent>();

        foreach (var human in humans)
        {
            var entity = human.Owner;

            if (!HasComp<ContentEyeComponent>(entity))
                continue;

            SetEyeZoom(entity, 1f);

            RemComp<TelescopeComponent>(entity);
        }
    }
}
