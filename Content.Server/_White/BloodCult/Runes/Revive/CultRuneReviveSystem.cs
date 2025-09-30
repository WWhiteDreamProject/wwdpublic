using System.Linq;
using Content.Server._White.GameTicking.Rules;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared._White.BloodCult.Runes.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._White.BloodCult.Runes.Revive;

public sealed class CultRuneReviveSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _eui = default!;

    [Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;
    [Dependency] private readonly BloodCultRuneSystem _bloodCultRune = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneReviveComponent, InvokeRuneEvent>(OnReviveRuneInvoked);
    }

    private void OnReviveRuneInvoked(Entity<CultRuneReviveComponent> ent, ref InvokeRuneEvent args)
    {
        var charges = _bloodCultRule.GetRevivalCharges();
        if (charges <= 0)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-rune-revive-no-charges"), args.User, args.User);
            args.Cancel();
            return;
        }

        var possibleTargets = _bloodCultRune.GetTargetsNearRune(
            ent,
            ent.Comp.ReviveRange,
            entity =>
                !HasComp<DamageableComponent>(entity) ||
                !HasComp<MobThresholdsComponent>(entity) ||
                !HasComp<MobStateComponent>(entity) ||
                _mobState.IsAlive(entity)
        );

        if (possibleTargets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-rune-no-targets"), args.User, args.User);
            args.Cancel();
            return;
        }

        var victim = possibleTargets.First();
        Revive(victim, args.User, ent);
        _bloodCultRule.SetRevivalCharges(charges - 1);
    }

    private void Revive(EntityUid target, EntityUid user, Entity<CultRuneReviveComponent> rune)
    {
        var deadThreshold = _threshold.GetThresholdForState(target, MobState.Dead);
        _damageable.TryChangeDamage(target, rune.Comp.Healing);

        if (!TryComp<DamageableComponent>(target, out var damageable) || damageable.TotalDamage > deadThreshold)
            return;

        _mobState.ChangeMobState(target, MobState.Critical, origin: user);
        if (!_mind.TryGetMind(target, out _, out var mind))
        {
            // if the mind is not found in the body, try to find the original cultist mind
            if (TryComp<BloodCultistComponent>(target, out var cultist) && cultist.OriginalMind != null)
                mind = cultist.OriginalMind.Value;
        }

        if (mind?.Session is not { } playerSession || mind.CurrentEntity == target)
            return;

        // notify them they're being revived.
        _eui.OpenEui(new ReturnToBodyEui(mind, _mind), playerSession);
    }
}
