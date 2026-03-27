using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware;
using Content.Server.Chat.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Server.Speech.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Server.Language;
using Content.Server.Mind;
using Content.Server.Chat.Managers;
using Content.Server._NC.Dispatch;
using Content.Shared.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._NC.Cyberware.Systems;

public sealed class CyberpsychosisSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StutteringSystem _stutteringSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly OverwatchSystem _overwatchSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HumanityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var humanity, out var xform))
        {
            // --- ТРИГГЕР СТАДИЙ ---
            
            if (TryComp<CyberpsychosisComponent>(uid, out var psycho))
            {
                // Если человечность восстановилась до 10 и выше (Безопасная зона или Стадия 1)
                if (humanity.CurrentHumanity >= 10f)
                {
                    // Если у нас была 3 стадия (роль антага), убираем её
                    if (HasComp<CyberpsychoRoleComponent>(uid))
                    {
                        RemoveStage3Antag(uid);
                    }

                    // Если человечность выше 20, убираем вообще весь психоз
                    if (humanity.CurrentHumanity > 20f)
                    {
                        _language.RemoveLanguage(uid, "Cyberpsycho");
                        _language.SetLanguage(uid, "Common");
                        RemCompDeferred<CyberpsychosisComponent>(uid);
                        continue;
                    }

                    // Если мы на 1 стадии (11-20), убираем кибер-язык
                    _language.RemoveLanguage(uid, "Cyberpsycho");
                    _language.SetLanguage(uid, "Common");
                }

                // Логика Стадии 2 (Человечность < 10)
                if (humanity.CurrentHumanity < 10f)
                {
                    _language.AddLanguage(uid, "Cyberpsycho");
                    _language.SetLanguage(uid, "Cyberpsycho");

                    psycho.GlitchTimer -= frameTime;
                    if (psycho.GlitchTimer <= 0f)
                    {
                        TriggerStage2Glitch(uid, xform);
                        psycho.GlitchTimer = _random.NextFloat(30f, 120f);
                    }
                }
            }
            else if (humanity.CurrentHumanity <= 20f)
            {
                AddComp<CyberpsychosisComponent>(uid);
            }

            // --- ФИНАЛЬНЫЙ ТРИГГЕР (СТАДИЯ 3: АНТАГОНИСТ) ---
            if (humanity.CurrentHumanity <= 0f && !HasComp<CyberpsychoRoleComponent>(uid))
            {
                TriggerStage3Antag(uid);
            }
        }
    }

    private void TriggerStage3Antag(EntityUid uid)
    {
        EnsureComp<CyberpsychoRoleComponent>(uid);
        Log.Info($"CYBERPSYCHOSIS: Stage 3 triggered for {ToPrettyString(uid)}");

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            _roleSystem.MindAddRole(mindId, "MindRoleCyberpsycho", mind);
            _mindSystem.TryAddObjective(mindId, mind, "CyberpsychoKillEveryoneObjective");

            if (_mindSystem.TryGetSession(mind, out var session))
            {
                _chatManager.DispatchServerMessage(session, "УБИТЬ ВСЕХ. ПЛОТЬ СЛАБА. ХРОМ ТРЕБУЕТ ЖЕРТВ.");
                _popupSystem.PopupEntity("ТВОЙ РАЗУМ ПОГАС. ОСТАЛСЯ ТОЛЬКО ХРОМ. УБИВАЙ.", uid, uid, PopupType.LargeCaution);
            }
        }

        _overwatchSystem.AddAlert(uid, "КРИТИЧЕСКИЙ СБОЙ: КИБЕРПСИХОЗ", "НЕИЗВЕСТНЫЙ СЕКТОР", true);
    }

    private void RemoveStage3Antag(EntityUid uid)
    {
        RemComp<CyberpsychoRoleComponent>(uid);
        Log.Info($"CYBERPSYCHOSIS: Stage 3 removed for {ToPrettyString(uid)}");

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            // Удаляем роль через маркерный компонент CyberpsychoRoleRoleComponent
            _roleSystem.MindTryRemoveRole<CyberpsychoRoleRoleComponent>(mindId);

            // Удаляем цели
            var objectives = mind.Objectives.ToList();
            for (var i = objectives.Count - 1; i >= 0; i--)
            {
                var objUid = objectives[i];
                if (MetaData(objUid).EntityPrototype?.ID == "CyberpsychoKillEveryoneObjective")
                {
                    _mindSystem.TryRemoveObjective(mindId, mind, i);
                }
            }

            if (_mindSystem.TryGetSession(mind, out var session))
            {
                _chatManager.DispatchServerMessage(session, "Твой разум прояснился. Жажда крови отступила.");
                _popupSystem.PopupEntity("Твой разум прояснился. Ты снова контролируешь себя.", uid, uid, PopupType.Medium);
            }
        }
    }

    private void TriggerStage2Glitch(EntityUid uid, TransformComponent xform)
    {
        var roll = _random.Next(1, 3);
        switch (roll)
        {
            case 1:
                if (TryComp<ActionsComponent>(uid, out var actions))
                {
                    var cyberActions = actions.Actions.AsEnumerable().Skip(2).ToList();
                    if (cyberActions.Count == 0) break;

                    foreach (var actionId in cyberActions)
                    {
                        if (!TryComp<InstantActionComponent>(actionId, out var action)) continue;
                        _actionsSystem.PerformAction(uid, actions, actionId, action, action.BaseEvent, _timing.CurTime);
                        _popupSystem.PopupEntity("Хром активировался сам по себе!", uid, uid, PopupType.MediumCaution);
                        break;
                    }
                }
                break;
            case 2:
                if (_handsSystem.TryGetActiveHand(uid, out var hand) && hand.HeldEntity != null)
                {
                    _popupSystem.PopupEntity("Пальцы судорожно разжались!", uid, uid, PopupType.MediumCaution);
                    _handsSystem.TryDrop(uid, hand);
                }
                break;
        }
    }

    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!TryComp<HumanityComponent>(args.Sender, out var humanity))
            return;

        if (humanity.CurrentHumanity <= 20f && humanity.CurrentHumanity >= 10f)
        {
            ApplyStage1Distortion(args);
        }
    }

    private void ApplyStage1Distortion(TransformSpeechEvent args)
    {
        var message = args.Message;
        if (_random.Prob(0.3f)) message = message.ToUpper();
        if (_random.Prob(0.3f))
        {
            var stutterComp = new StutteringAccentComponent();
            message = _stutteringSystem.Accentuate(message, stutterComp);
        }
        if (_random.Prob(0.2f) && _proto.TryIndex<CyberpsychosisTrashPrototype>("default", out var trashProto))
        {
            var key = _random.Pick(trashProto.Trash);
            message += " " + Loc.GetString(key);
        }
        args.Message = message;
    }
}