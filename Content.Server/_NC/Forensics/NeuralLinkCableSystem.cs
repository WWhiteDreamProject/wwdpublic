using System;
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
            _popup.PopupEntity("Target is not dead.", uid, args.User);
            return;
        }

        if (!TryComp<NeuralPortBufferComponent>(target, out var buffer))
        {
            _popup.PopupEntity("No neural port data.", uid, args.User);
            return;
        }

        if (buffer.TimeOfDeath == null)
        {
            _popup.PopupEntity("Neural port timing unavailable.", uid, args.User);
            return;
        }

        var delta = _timing.CurTime - buffer.TimeOfDeath.Value;
        if (delta.TotalMinutes > component.MaxDeathMinutes)
        {
            _popup.PopupEntity("Neural port has decayed.", uid, args.User);
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

        var timeOfDeath = buffer.TimeOfDeath?.ToString(@"hh\:mm\:ss") ?? "Unknown";
        var critical = buffer.LastCriticalDamage;

        var victimName = Name(target.Value);
        var lines = buffer.Lines
            .Take(component.MaxLogLines)
            .Select(l =>
            {
                var speaker = l.IsVictim ? victimName : "Anonymous";
                return $"[{l.Time:hh\\:mm\\:ss}] {speaker}: {l.Message}";
            })
            .ToList();

        if (lines.Count == 0)
            lines.Add("(no audio data)");

        var content = "--- NEURAL PORT LOG ---\n\n" +
                      $"TIME OF SHUTDOWN: {timeOfDeath}\n" +
                      $"CRITICAL DAMAGE: {critical}\n\n" +
                      "AUDIO BUFFER:\n" +
                      string.Join("\n", lines) +
                      "\n\n----------------------";

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
