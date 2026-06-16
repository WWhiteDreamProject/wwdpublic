using Content.Server._White.GameTicking.Rules.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.CCVar;
using Content.Shared._White.CCVar;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Configuration;

namespace Content.Server._White.GameTicking.Rules;

public sealed class DisablePreloadsRuleSystem : GameRuleSystem<DisablePreloadsRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        var query = EntityQueryEnumerator<DisablePreloadsRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            _cfg.SetCVar(CCVars.ArrivalsShuttles, comp.OriginalArrivalsShuttles);
            _cfg.SetCVar(WhiteCVars.AsteroidFieldEnabled, comp.OriginalAsteroidFieldEnabled);
            _cfg.SetCVar(CCVars.ProcgenPreload, comp.OriginalProcgenPreload);
            _cfg.SetCVar(CCVars.GridFill, comp.OriginalGridFill);
            _cfg.SetCVar(CCVars.PreloadGrids, comp.OriginalPreloadGrids);
            _cfg.SetCVar(CCVars.LavalandEnabled, comp.OriginalLavalandEnabled);
            _cfg.SetCVar(WhiteCVars.IsAspectsEnabled, comp.OriginalIsAspectsEnabled);

            GameTicker.EndGameRule(uid);
        }
    }

    protected override void Added(EntityUid uid, DisablePreloadsRuleComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, rule, args);

        comp.OriginalArrivalsShuttles = _cfg.GetCVar(CCVars.ArrivalsShuttles);
        comp.OriginalAsteroidFieldEnabled = _cfg.GetCVar(WhiteCVars.AsteroidFieldEnabled);
        comp.OriginalProcgenPreload = _cfg.GetCVar(CCVars.ProcgenPreload);
        comp.OriginalGridFill = _cfg.GetCVar(CCVars.GridFill);
        comp.OriginalPreloadGrids = _cfg.GetCVar(CCVars.PreloadGrids);
        comp.OriginalLavalandEnabled = _cfg.GetCVar(CCVars.LavalandEnabled);
        comp.OriginalIsAspectsEnabled = _cfg.GetCVar(WhiteCVars.IsAspectsEnabled);

        _cfg.SetCVar(CCVars.ArrivalsShuttles, false);
        _cfg.SetCVar(WhiteCVars.AsteroidFieldEnabled, false);
        _cfg.SetCVar(CCVars.ProcgenPreload, false);
        _cfg.SetCVar(CCVars.GridFill, false);
        _cfg.SetCVar(CCVars.PreloadGrids, false);
        _cfg.SetCVar(CCVars.LavalandEnabled, false);
        _cfg.SetCVar(WhiteCVars.IsAspectsEnabled, false);
    }

    protected override void Ended(EntityUid uid, DisablePreloadsRuleComponent comp, GameRuleComponent rule, GameRuleEndedEvent args) =>
        base.Ended(uid, comp, rule, args);
}
