using System.Linq;
using Content.Server._White.GameTicking.Aspects.Components;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.GameTicking.Aspects;

public sealed class TraitoredAspect : AspectSystem<TraitoredAspectComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRuleSystem = default!;
    [Dependency] private readonly WhiteGameTicker _whiteGameTicker = default!;

    protected override void Started(EntityUid uid, TraitoredAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var traitorRuleComponents = EntityQuery<TraitorRuleComponent>().ToList();
        if (traitorRuleComponents.Count == 0)
        {
            _whiteGameTicker.RunRandomAspect();
            ForceEndSelf(uid, gameRule);
            return;
        }

        component.TraitorRuleComponent = traitorRuleComponents.First();
        component.AnnouncedForAllAt = _timing.CurTime + _random.Next(component.AnnouncedForAllViaMin, component.AnnouncedForAllViaMax);
        component.AnnouncedForTraitorsAt = _timing.CurTime + component.AnnouncedForTraitorsVia;
    }

    protected override void ActiveTick(EntityUid uid, TraitoredAspectComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.TraitorRuleComponent?.SelectionStatus != TraitorRuleComponent.SelectionState.Started)
            return;

        if (!component.AnnouncedForTraitors && _timing.CurTime >= component.AnnouncedForTraitorsAt)
        {
            AnnounceToTraitors(uid, gameRule, component.AnnouncementForTraitorSound);
            component.AnnouncedForTraitors = true;
        }

        if (_timing.CurTime >= component.AnnouncedForAllAt)
            AnnounceToAll(uid, gameRule);
    }

    private void AnnounceToTraitors(EntityUid uid, GameRuleComponent rule, string sound)
    {
        var traitors = _traitorRuleSystem.GetAllLivingConnectedTraitors();

        if (traitors.Count == 0)
            ForceEndSelf(uid, rule);

        foreach (var traitor in traitors)
        {
            if (!_mindSystem.TryGetSession(traitor.Comp, out var session))
                continue;

            var mindOwned = traitor.Comp.OwnedEntity;

            if (mindOwned == null)
                return;

            _chatManager.DispatchServerMessage(session, Loc.GetString("aspect-traitored-briefing"));
            _audio.PlayEntity(sound, mindOwned.Value, mindOwned.Value);
        }
    }

    private void AnnounceToAll(EntityUid uid, GameRuleComponent rule)
    {
        var traitors = _traitorRuleSystem.GetAllLivingConnectedTraitors();

        var msg = Loc.GetString("aspect-traitored-announce");

        foreach (var traitor in traitors)
        {
            var name = traitor.Comp.CharacterName;
            if (!string.IsNullOrEmpty(name))
                msg += $"\n + {Loc.GetString("aspect-traitored-announce-name", ("name", name))}";
        }

        _chatSystem.DispatchGlobalAnnouncement(msg, Loc.GetString("aspect-traitored-announce-sender"), colorOverride: Color.Aquamarine);

        ForceEndSelf(uid, rule);
    }
}
