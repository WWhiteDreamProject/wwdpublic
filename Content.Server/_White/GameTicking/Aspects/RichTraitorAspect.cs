using Content.Server._White.GameTicking.Aspects.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;

namespace Content.Server._White.GameTicking.Aspects;

public sealed class RichTraitorAspect : AspectSystem<RichTraitorAspectComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRuleSystem = default!;
    [Dependency] private readonly UplinkSystem _uplinkSystem = default!;
    [Dependency] private readonly WhiteGameTicker _whiteGameTicker = default!;

    protected override void Started(EntityUid uid, RichTraitorAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!_gameTicker.IsGameRuleAdded<TraitorRuleComponent>())
        {
            _whiteGameTicker.RunRandomAspect();
            ForceEndSelf(uid, gameRule);
            return;
        }

        var traitors = _traitorRuleSystem.GetAllLivingConnectedTraitors();

        foreach (var traitor in traitors)
        {
            if (traitor.Comp.CurrentEntity is not { } ent
                || traitor.Comp.Session == null
                || _uplinkSystem.FindUplinkTarget(ent) is not { } uplink)
                continue;

            if (_store.TryAddCurrency(new Dictionary<string, FixedPoint2> {{UplinkSystem.TelecrystalCurrencyPrototype, 10}, }, uplink))
                _chatManager.DispatchServerMessage(traitor.Comp.Session, Robust.Shared.Localization.Loc.GetString("aspect-traitor-rich-briefing"));
        }
    }
}
