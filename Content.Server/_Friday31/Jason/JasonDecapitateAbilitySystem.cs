using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._Friday31.Jason;
using Content.Shared._Shitmed.Body.Events;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.CombatMode;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Friday31.Jason;

public sealed class JasonDecapitateAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JasonDecapitateAbilityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<JasonDecapitateAbilityComponent, DecapitateActionEvent>(OnDecapitate);
    }

    private void OnMapInit(EntityUid uid, JasonDecapitateAbilityComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnDecapitate(EntityUid uid, JasonDecapitateAbilityComponent component, DecapitateActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<CombatModeComponent>(uid, out var combatMode) || !combatMode.IsInCombatMode)
            return;

        var target = args.Target;

        if (!TryComp<BodyComponent>(target, out var body))
            return;

        EntityUid? headPart = null;
        foreach (var (partId, part) in _body.GetBodyChildren(target, body))
        {
            if (part.PartType == BodyPartType.Head)
            {
                headPart = partId;
                break;
            }
        }

        if (headPart == null)
        {
            _popup.PopupEntity(Loc.GetString("jason-decapitate-no-head"), uid, uid);
            return;
        }

        _chat.TryEmoteWithChat(target, "Scream");

        var ev = new AmputateAttemptEvent(headPart.Value);
        RaiseLocalEvent(headPart.Value, ref ev);

        if (args.Sound != null)
            _audio.PlayPvs(args.Sound, uid);

        _popup.PopupEntity(Loc.GetString("jason-decapitate-success", ("target", target)), uid, PopupType.LargeCaution);

        args.Handled = true;
    }
}
