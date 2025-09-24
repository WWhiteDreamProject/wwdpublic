using Content.Server._White.GameTicking.Rules;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared.Interaction;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.BloodCult.ShuttleCurse;

public sealed class ShuttleCurseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleCurseComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(Entity<ShuttleCurseComponent> orb, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        var charges = _bloodCultRule.GetShuttleCurseCharges();
        if (charges <= 0)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-curse-max-charges"), orb, args.User);
            return;
        }

        if (_emergencyShuttle.EmergencyShuttleArrived)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-curse-shuttle-arrived"), orb, args.User);
            return;
        }

        if (_roundEnd.ExpectedCountdownEnd is null)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-curse-shuttle-not-called"), orb, args.User);
            return;
        }

        _roundEnd.DelayShuttle(orb.Comp.DelayTime);

        var message = string.Empty;
        if (_prototypeManager.TryIndex(orb.Comp.CurseMessages, out var messages))
            message = _random.Pick(messages.Values);

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("shuttle-curse-success-global", ("message", message), ("time", orb.Comp.DelayTime.TotalMinutes)),
            Loc.GetString("shuttle-curse-system-failure"),
            colorOverride: Color.Gold);

        _popup.PopupEntity(Loc.GetString("shuttle-curse-success"), args.User, args.User);
        _bloodCultRule.SetShuttleCurseCharges(charges - 1);

        _audio.PlayEntity(orb.Comp.ScatterSound, Filter.Pvs(orb), orb, true);
        Del(orb);
    }
}
