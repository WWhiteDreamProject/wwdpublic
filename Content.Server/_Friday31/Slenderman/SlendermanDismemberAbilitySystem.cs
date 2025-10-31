using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._Friday31.Slenderman;
using Content.Shared._Shitmed.Body.Events;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Log;
using Robust.Shared.Random;

namespace Content.Server._Friday31.Slenderman;

public sealed class SlendermanDismemberAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ISawmill _sawmill = default!;

    private static readonly string[] FleshSounds = new[]
    {
        "/Audio/Weapons/Xeno/alien_claw_flesh1.ogg",
        "/Audio/Weapons/Xeno/alien_claw_flesh2.ogg",
        "/Audio/Weapons/Xeno/alien_claw_flesh3.ogg"
    };

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("slenderman.dismember");
        SubscribeLocalEvent<SlendermanDismemberAbilityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlendermanDismemberAbilityComponent, SlendermanDismemberActionEvent>(OnDismember);
    }

    private void OnMapInit(EntityUid uid, SlendermanDismemberAbilityComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnDismember(EntityUid uid, SlendermanDismemberAbilityComponent component, SlendermanDismemberActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryComp<BodyComponent>(target, out var body))
        {
            return;
        }

        var partsToRemove = new List<EntityUid>();
        foreach (var (partId, part) in _body.GetBodyChildren(target, body))
        {
            if (part.PartType is BodyPartType.Arm or BodyPartType.Hand or BodyPartType.Leg or BodyPartType.Foot or BodyPartType.Head)
            {
                partsToRemove.Add(partId);
            }
        }

        if (partsToRemove.Count == 0)
        {
            return;
        }

        _chat.TryEmoteWithChat(target, "Scream");
        if (TryComp<ActorComponent>(target, out var actor))
        {
            var screamerEvent = new SlendermanScreamerEvent(component.ScreamerDuration);
            RaiseNetworkEvent(screamerEvent, actor.PlayerSession);
            _sawmill.Info($"Sent screamer event to player {actor.PlayerSession.Name} with duration {component.ScreamerDuration}");
        }
        else
        {
            _sawmill.Warning($"Target {target} has no ActorComponent, screamer not sent!");
        }

        if (args.DismemberSound != null)
        {
            _audio.PlayPvs(args.DismemberSound, uid, AudioParams.Default.WithMaxDistance(args.SoundRange));
        }

        var fleshSound = _random.Pick(FleshSounds);
        _audio.PlayPvs(fleshSound, target);

        foreach (var part in partsToRemove)
        {
            var ev = new AmputateAttemptEvent(part);
            RaiseLocalEvent(part, ref ev);
        }

        args.Handled = true;
    }
}
