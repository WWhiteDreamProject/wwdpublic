using System;
using Robust.Shared.Localization;
using System.Linq;
using Content.Shared._NC.Forensics;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Server.Popups;
using Content.Shared.Paper;
using Robust.Shared.Timing;
using Content.Server.Hands.Systems;

namespace Content.Server._NC.Forensics;

public sealed class NeuralLinkCableSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NeuralLinkCableComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<NeuralLinkCableComponent, NeuralLinkDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, NeuralLinkCableComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<MobStateComponent>(target, out var mob) || mob.CurrentState != MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("forensics-neural-target-not-dead"), uid, args.User);
            return;
        }

        if (!TryComp<NeuralPortBufferComponent>(target, out var buffer))
        {
            _popup.PopupEntity(Loc.GetString("forensics-neural-no-data"), uid, args.User);
            return;
        }

        if (buffer.TimeOfDeath == null)
        {
            _popup.PopupEntity(Loc.GetString("forensics-neural-no-timing"), uid, args.User);
            return;
        }

        var delta = _timing.CurTime - buffer.TimeOfDeath.Value;
        if (delta.TotalMinutes > component.MaxDeathMinutes)
        {
            _popup.PopupEntity(Loc.GetString("forensics-neural-decayed"), uid, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.DurationSeconds), new NeuralLinkDoAfterEvent(), uid, target: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, NeuralLinkCableComponent component, NeuralLinkDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var target = args.Args.Target;
        if (target == null)
            return;

        if (!TryComp<NeuralPortBufferComponent>(target.Value, out var buffer))
            return;

        var unknown = Loc.GetString("forensics-neural-unknown");
        var timeOfDeath = buffer.TimeOfDeath?.ToString(@"hh\:mm\:ss") ?? unknown;
        var critical = buffer.LastCriticalDamage;

        var victimName = Name(target.Value);
        var lines = buffer.Lines
            .Take(component.MaxLogLines)
            .Select(l =>
            {
                var speaker = l.IsVictim ? victimName : Loc.GetString("forensics-neural-anonymous");
                return Loc.GetString("forensics-neural-log-line", 
                    ("time", l.Time.ToString(@"hh\:mm\:ss")), 
                    ("speaker", speaker), 
                    ("message", l.Message));
            })
            .ToList();

        if (lines.Count == 0)
            lines.Add(Loc.GetString("forensics-neural-no-audio"));

        var content = Loc.GetString("forensics-neural-log-header") + "\n\n" +
                      Loc.GetString("forensics-neural-log-shutdown", ("time", timeOfDeath)) + "\n" +
                      Loc.GetString("forensics-neural-log-damage", ("damage", critical)) + "\n\n" +
                      Loc.GetString("forensics-neural-log-audio") + "\n" +
                      string.Join("\n", lines) + "\n\n" +
                      Loc.GetString("forensics-report-separator");

        var coords = Transform(args.Args.User).Coordinates;
        var paper = Spawn("Paper", coords);
        if (TryComp<PaperComponent>(paper, out var paperComp))
        {
            _paper.SetContent((paper, paperComp), content);
        }

        _hands.TryPickupAnyHand(args.Args.User, paper, checkActionBlocker: false);
        args.Handled = true;
    }
}
