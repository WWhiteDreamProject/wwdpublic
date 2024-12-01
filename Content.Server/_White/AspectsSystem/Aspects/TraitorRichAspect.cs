using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server._White.AspectsSystem.Managers;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class TraitorRichAspect : AspectSystem<TraitorRichAspectComponent>
{
    [Dependency] private readonly TraitorRuleSystem _traitorRuleSystem = default!;
    [Dependency] private readonly UplinkSystem _uplinkSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly AspectManager _aspectManager = default!;

    protected override void Started(EntityUid uid, TraitorRichAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!HasTraitorGameRule())
            _aspectManager.RunRandomAspect();

        RewardTraitors();
    }

    private void RewardTraitors()
    {
        var traitors = _traitorRuleSystem.GetAllLivingConnectedTraitors();

        foreach (var traitor in traitors)
        {
            var ent = traitor.Mind.CurrentEntity;

            if (ent == null)
                continue;

            var uplink = _uplinkSystem.FindUplinkTarget(ent.Value);

            if (uplink == null || !TryComp(uplink, out StoreComponent? store) || store.AccountOwner != ent || store.Preset != "StorePresetUplink")
                continue;

            if (_store.TryAddCurrency(new Dictionary<string, FixedPoint2> {{UplinkSystem.TelecrystalCurrencyPrototype, 10}}, uplink.Value, store))
                NotifyTraitor(traitor.Mind, _chatManager);
        }
    }

    public static void NotifyTraitor(MindComponent mind, IChatManager chatManager)
    {
        if (mind.Session == null)
            return;

        chatManager.DispatchServerMessage(mind.Session, Robust.Shared.Localization.Loc.GetString("aspect-traitor-rich-briefing"));
    }

    private bool HasTraitorGameRule() => EntityQuery<TraitorRuleComponent>().Any();
}
