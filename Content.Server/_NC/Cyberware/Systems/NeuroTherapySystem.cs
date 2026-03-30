using System.Globalization;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Stunnable;
using Content.Shared._NC.Cyberware;
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware.Systems;
using Content.Shared._NC.Cyberware.UI;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NC.Cyberware.Systems;

/// <summary>
/// Парсит локальный чат вокруг пациента, подключённого к биомонитору, и применяет эффекты слов-триггеров.
/// </summary>
public sealed class NeuroTherapySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly HumanitySystem _humanity = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BiomonitorComponent, AfterInteractEvent>(OnBiomonitorInteract);
        SubscribeLocalEvent<BiomonitorComponent, DroppedEvent>(OnBiomonitorDropped);
        SubscribeLocalEvent<BiomonitorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TransformComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnBiomonitorInteract(Entity<BiomonitorComponent> monitor, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        if (!HasComp<HumanityComponent>(target))
        {
            _popup.PopupEntity("Пациент не поддаётся анализу биомонитором.", args.User, args.User, PopupType.MediumCaution);
            return;
        }

        StartSession(monitor.Owner, monitor.Comp, target);

        // сразу открываем интерфейс врачу
        if (_ui.TryOpenUi(monitor.Owner, BiomonitorUiKey.Key, args.User))
            UpdateUiState(monitor.Owner, monitor.Comp);

        args.Handled = true;
    }

    private void OnBiomonitorDropped(Entity<BiomonitorComponent> monitor, ref DroppedEvent args)
    {
        DisconnectMonitor(monitor.Owner, monitor.Comp);
    }

    private void OnUseInHand(Entity<BiomonitorComponent> monitor, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (monitor.Comp.ConnectedPatient is not { } patient || !TryComp<HumanityComponent>(patient, out var humanity))
        {
            _popup.PopupEntity("Биомонитор не подключён к пациенту.", args.User, args.User, PopupType.MediumCaution);
            return;
        }

        if (!_ui.TryOpenUi(monitor.Owner, BiomonitorUiKey.Key, args.User))
            return;

        UpdateUiState(monitor.Owner, monitor.Comp);

        args.Handled = true;
    }

    private void OnEntitySpoke(EntityUid uid, TransformComponent xform, ref EntitySpokeEvent ev)
    {
        if (ev.Channel != null)
            return;

        if (ev.Message.StartsWith("[Биомонитор]:", StringComparison.Ordinal))
            return;

        var words = ExtractWords(ev.Message);
        if (words.Count == 0)
            return;

        var sourceCoords = xform.Coordinates;

        // Срабатывает всегда по словам пациента, даже без биомонитора.
        if (TryComp<HumanityComponent>(uid, out var selfHumanity) && ProcessMessage(null, null, uid, selfHumanity, words))
        {
            if (selfHumanity.ActiveBiomonitor is { } selfMon && TryComp<BiomonitorComponent>(selfMon, out var selfMonComp))
                UpdateUiState(selfMon, selfMonComp);
            return;
        }

        // Локальные мониторы вокруг говорящего пациента
        var query = EntityQueryEnumerator<BiomonitorComponent>();
        while (query.MoveNext(out var monitorUid, out var monitor))
        {
            if (monitor.ConnectedPatient is not { } patient)
                continue;
            if (!TryComp<HumanityComponent>(patient, out var humanity))
                continue;
            if (!TryComp(patient, out TransformComponent? patientXform))
                continue;

            if (!_transform.InRange(patientXform.Coordinates, sourceCoords, monitor.ListenRange))
                continue;

            if (ProcessMessage(monitorUid, monitor, patient, humanity, words))
            {
                UpdateUiState(monitorUid, monitor);
                break;
            }
        }
    }

    private void StartSession(EntityUid monitorUid, BiomonitorComponent monitor, EntityUid patient)
    {
        if (!TryComp<HumanityComponent>(patient, out var humanity))
            return;

        if (humanity.ActiveBiomonitor is { } other && other != monitorUid && TryComp<BiomonitorComponent>(other, out var oldMonitor))
            DisconnectMonitor(other, oldMonitor);

        monitor.ConnectedPatient = patient;
        humanity.ActiveBiomonitor = monitorUid;
        Dirty(patient, humanity);

    }

    private void DisconnectMonitor(EntityUid monitorUid, BiomonitorComponent monitor)
    {
        if (monitor.ConnectedPatient is { } patient && TryComp<HumanityComponent>(patient, out var humanity) && humanity.ActiveBiomonitor == monitorUid)
        {
            humanity.ActiveBiomonitor = null;
            Dirty(patient, humanity);
        }

        monitor.ConnectedPatient = null;
    }

    private bool ProcessMessage(EntityUid? monitorUid, BiomonitorComponent? monitor, EntityUid patient, HumanityComponent humanity, IReadOnlyList<string> words)
    {
        foreach (var word in words)
        {
            if (humanity.TargetTraumaWords.Contains(word))
            {
                TriggerTrauma(monitorUid, monitor, patient, humanity, word);
                return true;
            }

            if (humanity.TargetHealingWords.Contains(word))
            {
                TriggerHealing(monitorUid, monitor, patient, humanity, word);
                return true;
            }
        }

        return false;
    }

    private void TriggerHealing(EntityUid? monitorUid, BiomonitorComponent? monitor, EntityUid patient, HumanityComponent humanity, string word)
    {
        _humanity.RestoreHumanity(patient, 3f, humanity);
        _humanity.ReduceMaxHumanity(patient, 3f, humanity);
        humanity.TargetHealingWords.Remove(word);
        Dirty(patient, humanity);

        SendEmote(patient);
    }

    private void TriggerTrauma(EntityUid? monitorUid, BiomonitorComponent? monitor, EntityUid patient, HumanityComponent humanity, string word)
    {
        _humanity.DeductHumanity(patient, 3f, humanity);
        _humanity.ReduceMaxHumanity(patient, 3f, humanity);
        humanity.TargetTraumaWords.Remove(word);
        _stamina.TakeStaminaDamage(patient, 25f, visual: false);
        _stun.TryParalyze(patient, TimeSpan.FromSeconds(1.5f), true);
        Dirty(patient, humanity);

        SendEmote(patient);
    }

    private void UpdateUiState(EntityUid monitorUid, BiomonitorComponent monitor)
    {
        if (monitor.ConnectedPatient is not { } patient || !TryComp<HumanityComponent>(patient, out var humanity))
            return;

        _ui.SetUiState(monitorUid, BiomonitorUiKey.Key, new BiomonitorBoundUserInterfaceState(
            humanity.TargetHealingWords.Count,
            humanity.TargetTraumaWords.Count,
            humanity.CurrentHumanity,
            humanity.MaxHumanity));
    }

    private void SendEmote(EntityUid patient)
    {
        if (!_prototypes.TryIndex<TherapyEmotePrototype>("default", out var emotes) || emotes.Lines.Count == 0)
            return;

        var line = _random.Pick(emotes.Lines);
        _chat.TrySendInGameICMessage(patient, line, InGameICChatType.Emote, hideChat: false);
    }

    public void AssignWords(EntityUid patient, int healingCount, int traumaCount)
    {
        if (!TryComp<HumanityComponent>(patient, out var humanity))
            return;

        var pool = _prototypes.EnumeratePrototypes<TherapyWordPoolPrototype>().FirstOrDefault();
        if (pool == null || pool.Words.Count == 0)
            return;

        var allWords = pool.Words
            .Select(w => w.Trim().ToLower(CultureInfo.InvariantCulture))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct()
            .ToList();

        void AddWords(List<string> target, int count)
        {
            var available = allWords.Where(w => !humanity.TargetHealingWords.Contains(w) && !humanity.TargetTraumaWords.Contains(w)).ToList();
            for (var i = 0; i < count; i++)
            {
                if (available.Count == 0)
                    available = allWords;
                var word = _random.Pick(available);
                target.Add(word);
                available.Remove(word);
            }
        }

        AddWords(humanity.TargetHealingWords, healingCount);
        AddWords(humanity.TargetTraumaWords, traumaCount);
        Dirty(patient, humanity);
    }

    private static List<string> ExtractWords(string message)
    {
        var words = new List<string>();
        var lower = message.ToLower(CultureInfo.InvariantCulture);
        var span = lower.AsSpan();

        var start = -1;
        for (var i = 0; i < span.Length; i++)
        {
            if (char.IsLetterOrDigit(span[i]))
            {
                if (start == -1)
                    start = i;
                continue;
            }

            if (start != -1)
            {
                words.Add(span.Slice(start, i - start).ToString());
                start = -1;
            }
        }

        if (start != -1)
            words.Add(span.Slice(start).ToString());

        return words;
    }
}
