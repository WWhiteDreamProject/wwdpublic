using System.Linq;
using Content.Shared._NC.Decryption.Components;
using Content.Shared._NC.Decryption.UI;
using Content.Shared._NC.Weapons.Ranged.NCWeapon;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server._NC.Decryption;

// Server-authoritative decryption minigame terminal.
public sealed class DecryptionSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, DecryptionSession> _sessions = new();

    private const int BaseIntegrity = 100;

    private static readonly string[] ProtocolTitles =
    {
        "ROBCO INDUSTRIES UNIFIED OPERATING SYSTEM",
        "MILITECH ICE PROTOCOL v2.0",
        "NETWORK ENTRY INTERFACE // SECURE MODE",
        "DATA-ICE DECRYPTION SUBROUTINE",
    };

    private static readonly string[] WordPool =
    {
        "CHROME", "WEAPON", "MATRIX", "CIPHER", "BREACH", "CYBER", "SCRIPT", "HOST", "GRID", "NODE",
        "PROXY", "SHARD", "SENTRY", "GATE", "GHOST", "ACCESS", "KERNEL", "INTRUDE", "NETWORK", "PAYLOAD",
        "FIREWALL", "OVERRIDE", "ENCRYPT", "DECRYPT", "PROTOCOL", "BACKDOOR", "BLACKICE", "WHITEICE",
        "SYNAPSE", "DATAVAULT", "MAINFRAME", "TERMINAL", "INJECTOR", "SCANLINE", "CHECKSUM", "VECTOR",
        "NEXUS", "NANOWIRE", "MEGACORP", "ICEBREAKER", "NEURALINK", "CYBERDECK", "DATASHARD", "GIGAFRAME"
    };

    private const string JunkSymbols = "!@#$%^&*;:+-=?/|~";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DecryptionTerminalComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DecryptionTerminalComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<DecryptionTerminalComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<DecryptionTerminalComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DecryptionTerminalComponent, BoundUIClosedEvent>(OnUiClosed);

        SubscribeLocalEvent<DecryptionTerminalComponent, DecryptionStartMessage>(OnStartDecryption);
        SubscribeLocalEvent<DecryptionTerminalComponent, DecryptionMatrixClickMessage>(OnMatrixClick);
        SubscribeLocalEvent<DecryptionTerminalComponent, DecryptionEjectCarrierMessage>(OnEjectCarrier);
        SubscribeLocalEvent<DecryptionTechnologyComponent, ExaminedEvent>(OnTechnologyExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var refresh = new List<EntityUid>();
        var expired = new List<EntityUid>();

        foreach (var (uid, session) in _sessions)
        {
            if (!session.TimerActive)
                continue;

            session.TimerRemaining = MathF.Max(0f, session.TimerRemaining - frameTime);
            var sec = (int) MathF.Ceiling(session.TimerRemaining);
            if (sec != session.LastDisplayedTimerSeconds)
            {
                session.LastDisplayedTimerSeconds = sec;
                refresh.Add(uid);
            }

            if (session.TimerRemaining <= 0f)
                expired.Add(uid);
        }

        foreach (var uid in refresh)
        {
            if (TryComp<DecryptionTerminalComponent>(uid, out var component))
                UpdateUi(uid, component);
        }

        foreach (var uid in expired)
        {
            if (!_sessions.TryGetValue(uid, out var session) || !TryComp<DecryptionTerminalComponent>(uid, out var component))
                continue;

            session.Log.Add("> ACTIVE ICE TIMEOUT.");
            session.Log.Add("> CARRIER BURNED.");
            CompleteFailure(uid, component);
        }
    }

    private void OnInteractUsing(EntityUid uid, DecryptionTerminalComponent component, InteractUsingEvent args)
    {
        if (!HasComp<DecryptionTechnologyComponent>(args.Used) || TryGetCarrier(uid, out _))
            return;

        if (_itemSlots.TryInsert(uid, DecryptionTerminalComponent.DataSlotId, args.Used, args.User))
        {
            args.Handled = true;
            _ui.TryOpenUi(uid, DecryptionUiKey.Key, args.User);
            UpdateUi(uid, component);
        }
    }

    private void OnContainerModified(EntityUid uid, DecryptionTerminalComponent component, ContainerModifiedMessage args)
    {
        _sessions.Remove(uid);
        UpdateUi(uid, component);
    }

    private void OnUiOpened(EntityUid uid, DecryptionTerminalComponent component, BoundUIOpenedEvent args)
    {
        UpdateUi(uid, component);
    }

    private void OnUiClosed(EntityUid uid, DecryptionTerminalComponent component, BoundUIClosedEvent args)
    {
        if (args.UiKey is DecryptionUiKey)
            _sessions.Remove(uid);
    }

    private void OnStartDecryption(EntityUid uid, DecryptionTerminalComponent component, DecryptionStartMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!TryGetCarrier(uid, out var carrierUid) || carrierUid is not { } carrier)
            return;

        if (!TryComp<DecryptionTechnologyComponent>(carrier, out var technology))
        {
            UpdateUi(uid, component, new List<string>
            {
                "> INSERT TECHNOLOGY PAYLOAD.",
                "> RAW DATA IS NOT SUPPORTED HERE."
            });
            return;
        }

        if (technology.IsDecrypted)
        {
            _sessions.Remove(uid);
            UpdateUi(uid, component, new List<string>
            {
                "> TECHNOLOGY ALREADY DECRYPTED.",
                "> INSERT NEW TECHNOLOGY PAYLOAD."
            });
            return;
        }

        _sessions[uid] = CreateSession(actor, carrier, technology, component);
        UpdateUi(uid, component);
    }

    private void OnMatrixClick(EntityUid uid, DecryptionTerminalComponent component, DecryptionMatrixClickMessage args)
    {
        if (!_sessions.TryGetValue(uid, out var session))
            return;

        if (args.CellIndex < 0 || args.CellIndex >= session.MatrixCells.Count)
            return;

        if (session.BackdoorByCell.TryGetValue(args.CellIndex, out var backdoorId))
        {
            HandleBackdoorClick(uid, component, session, backdoorId);
            return;
        }

        if (session.WordByCell.TryGetValue(args.CellIndex, out var word))
            HandleWordGuess(uid, component, session, word);
    }

    private void HandleWordGuess(EntityUid uid, DecryptionTerminalComponent component, DecryptionSession session, string word)
    {
        if (session.RemovedWords.Contains(word) || !session.Words.Contains(word))
            return;

        session.Log.Add($"> INPUT: {word}");

        if (word == session.Password)
        {
            session.Log.Add("> ACCESS GRANTED.");
            session.Log.Add($"> DECRYPTION SUCCESS. INTEGRITY: {session.Integrity}%.");
            CompleteSuccess(uid, component, session);
            return;
        }

        session.AttemptsRemaining = Math.Max(0, session.AttemptsRemaining - 1);
        session.PermanentMistakes++;

        var match = CountExactPositionMatches(word, session.Password);
        session.Log.Add("> ACCESS DENIED.");
        session.Log.Add($"> MATCH: {match}/{session.Password.Length}.");

        if (session.AttemptsRemaining <= 0)
        {
            session.Log.Add("> DEFENSIVE ICE TRIGGERED.");
            session.Log.Add("> CARRIER BURNED.");
            CompleteFailure(uid, component);
            return;
        }

        TrimLog(session.Log, session.LogLineLimit);
        UpdateUi(uid, component);
    }

    private void HandleBackdoorClick(EntityUid uid, DecryptionTerminalComponent component, DecryptionSession session, int backdoorId)
    {
        var backdoor = session.Backdoors.FirstOrDefault(b => b.Id == backdoorId);
        if (backdoor == null || backdoor.Used)
            return;

        backdoor.Used = true;

        if (_random.Prob(0.8f))
        {
            var candidates = session.Words
                .Where(w => w != session.Password && !session.RemovedWords.Contains(w))
                .ToList();

            if (candidates.Count > 0)
            {
                var removed = _random.Pick(candidates);
                RemoveDudFromMatrix(session, removed);
                session.Log.Add("> DUD REMOVED.");
            }
            else
            {
                session.Log.Add("> BACKDOOR EMPTY: NO DUDS LEFT.");
            }
        }
        else
        {
            session.AttemptsRemaining = session.MaxAttempts;
            session.PermanentMistakes = 0;
            session.Log.Add("> ATTEMPTS REPLENISHED.");
        }

        TrimLog(session.Log, session.LogLineLimit);
        UpdateUi(uid, component);
    }

    private static void RemoveDudFromMatrix(DecryptionSession session, string removedWord)
    {
        session.RemovedWords.Add(removedWord);

        var wordCells = session.WordByCell
            .Where(x => x.Value == removedWord)
            .Select(x => x.Key)
            .ToList();

        foreach (var index in wordCells)
        {
            session.WordByCell.Remove(index);

            if (index < 0 || index >= session.MatrixCells.Count)
                continue;

            var cell = session.MatrixCells[index];
            cell.Word = string.Empty;
            cell.Glyph = '.';
        }
    }

    private static void OnTechnologyExamined(EntityUid uid, DecryptionTechnologyComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var status = component.IsDecrypted ? "Decrypted" : "Encrypted";
        args.PushMarkup($"Technology: {status}");
    }
    private void OnEjectCarrier(EntityUid uid, DecryptionTerminalComponent component, DecryptionEjectCarrierMessage args)
    {
        _sessions.Remove(uid);

        if (args.Actor is not { Valid: true } actor)
            return;

        _itemSlots.TryEjectToHands(uid, component.DataSlot, actor);
        UpdateUi(uid, component);
    }

    private void CompleteSuccess(EntityUid uid, DecryptionTerminalComponent component, DecryptionSession session)
    {
        if (TryComp<DecryptionTechnologyComponent>(session.Carrier, out var technology))
        {
            technology.IsDecrypted = true;
            technology.DecryptedIntegrity = session.Integrity;
            technology.RemainingUses = session.SelectedTechnologyUses;
            Dirty(session.Carrier, technology);
        }

        _sessions.Remove(uid);
        UpdateUi(uid, component, new List<string>
        {
            "> DECRYPTION COMPLETE.",
            $"> FINAL INTEGRITY: {session.Integrity}%.",
            $"> TECHNOLOGY ENTITY: {session.SelectedTechnologyEntityId}",
            $"> USES: {session.SelectedTechnologyUses}."
        });
    }

    private void CompleteFailure(EntityUid uid, DecryptionTerminalComponent component)
    {
        if (!TryGetCarrier(uid, out var carrierUid) ||
            carrierUid is not { } carrier ||
            !_container.TryGetContainer(uid, DecryptionTerminalComponent.DataSlotId, out var slotContainer))
        {
            _sessions.Remove(uid);
            UpdateUi(uid, component);
            return;
        }

        _container.Remove(carrier, slotContainer);
        QueueDel(carrier);

        var burned = Spawn(component.BurnedMediaPrototype, Transform(uid).Coordinates);
        _container.Insert(burned, slotContainer);

        _sessions.Remove(uid);
        UpdateUi(uid, component);
    }

    private bool TryGetCarrier(EntityUid terminal, out EntityUid? carrier)
    {
        carrier = null;

        if (!_container.TryGetContainer(terminal, DecryptionTerminalComponent.DataSlotId, out var slotContainer) ||
            slotContainer.ContainedEntities.Count <= 0)
        {
            return false;
        }

        carrier = slotContainer.ContainedEntities[0];
        return true;
    }

    private DecryptionSession CreateSession(
        EntityUid user,
        EntityUid carrier,
        DecryptionTechnologyComponent technology,
        DecryptionTerminalComponent terminal)
    {
        var tier = technology.Tier;
        var (wordLength, wordCount, backdoorCount) = GetTierParams(tier);

        var words = PickWords(wordLength, wordCount);
        var password = _random.Pick(words);

        var enableTimer = terminal.ActiveIceTimeoutSeconds > 0 &&
                          (!terminal.ActiveIceLegendaryOnly || tier == WeaponTier.Legendary);

        var session = new DecryptionSession
        {
            User = user,
            Carrier = carrier,
            Tier = tier,
            ProtocolTitle = _random.Pick(ProtocolTitles),
            Password = password,
            SelectedTechnologyEntityId = MetaData(carrier).EntityPrototype?.ID ?? "unknown",
            SelectedTechnologyUses = Math.Max(1, technology.MaxUses),
            Words = words,
            MatrixWidth = Math.Max(8, terminal.MatrixWidth),
            MatrixHeight = Math.Max(8, terminal.MatrixHeight),
            MaxAttempts = Math.Max(1, terminal.MaxAttempts),
            AttemptsRemaining = Math.Max(1, terminal.MaxAttempts),
            IntegrityDamagePerMistake = Math.Max(1, terminal.AttemptIntegrityDamage),
            LogLineLimit = Math.Max(5, terminal.LogLineLimit),
            TimerActive = enableTimer,
            TimerRemaining = enableTimer ? terminal.ActiveIceTimeoutSeconds : 0f,
            LastDisplayedTimerSeconds = enableTimer ? terminal.ActiveIceTimeoutSeconds : -1
        };

        BuildMatrix(session, words, backdoorCount);

        session.Log.Add("> INITIALIZING PROTOCOL...");
        session.Log.Add($"> TARGET TIER: {tier}");
        session.Log.Add($"> TARGET TECH ENTITY: {session.SelectedTechnologyEntityId}");
        if (enableTimer)
            session.Log.Add($"> ACTIVE ICE TIMEOUT: {terminal.ActiveIceTimeoutSeconds}s.");
        session.Log.Add("> CLICK MATRIX SYMBOLS TO PROBE ICE.");
        TrimLog(session.Log, session.LogLineLimit);

        return session;
    }

    private (int WordLength, int WordCount, int BackdoorCount) GetTierParams(WeaponTier tier)
    {
        return tier switch
        {
            WeaponTier.Standard => (_random.Next(4, 6), _random.Next(6, 9), _random.Next(5, 8)),
            WeaponTier.Excellent => (_random.Next(6, 9), _random.Next(10, 15), _random.Next(3, 5)),
            WeaponTier.Legendary => (_random.Next(9, 13), _random.Next(16, 21), _random.Next(1, 3)),
            _ => (_random.Next(4, 6), _random.Next(6, 9), _random.Next(5, 8))
        };
    }

    private List<string> PickWords(int length, int count)
    {
        var pool = WordPool
            .Where(w => w.Length == length)
            .Distinct()
            .ToList();

        while (pool.Count < count)
        {
            var generated = GenerateRandomWord(length);
            if (!pool.Contains(generated))
                pool.Add(generated);
        }

        var result = new List<string>(count);
        while (result.Count < count && pool.Count > 0)
        {
            var candidate = _random.Pick(pool);
            pool.Remove(candidate);
            result.Add(candidate);
        }

        return result;
    }

    private string GenerateRandomWord(int length)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[_random.Next(alphabet.Length)];
        }

        return new string(chars);
    }

    private void BuildMatrix(DecryptionSession session, List<string> words, int backdoorCount)
    {
        var totalCells = session.MatrixWidth * session.MatrixHeight;
        session.MatrixCells.Clear();
        session.WordByCell.Clear();
        session.BackdoorByCell.Clear();
        session.Backdoors.Clear();

        var occupied = new bool[totalCells];

        for (var i = 0; i < totalCells; i++)
        {
            session.MatrixCells.Add(new DecryptionSession.MatrixCellData
            {
                Glyph = JunkSymbols[_random.Next(JunkSymbols.Length)],
                Word = string.Empty,
                BackdoorId = -1
            });
        }

        foreach (var word in words)
        {
            if (!TryPlaceToken(session, word, occupied, out var indices))
                continue;

            foreach (var index in indices)
                session.WordByCell[index] = word;
        }

        var tokens = GenerateBackdoorTokens(backdoorCount);
        for (var backdoorId = 0; backdoorId < tokens.Count; backdoorId++)
        {
            var token = tokens[backdoorId];
            if (!TryPlaceToken(session, token, occupied, out var indices))
                continue;

            session.Backdoors.Add(new DecryptionSession.BackdoorData
            {
                Id = backdoorId,
                Token = token,
            });

            foreach (var index in indices)
            {
                if (index < 0 || index >= session.MatrixCells.Count)
                    continue;

                var cell = session.MatrixCells[index];
                cell.BackdoorId = backdoorId;
            }

            if (indices.Count > 0)
                session.BackdoorByCell[indices[0]] = backdoorId;
        }
    }

    private bool TryPlaceToken(DecryptionSession session, string token, bool[] occupied, out List<int> indices)
    {
        indices = new List<int>(token.Length);

        for (var attempt = 0; attempt < 300; attempt++)
        {
            var row = _random.Next(session.MatrixHeight);
            var startColumn = _random.Next(0, session.MatrixWidth - token.Length + 1);

            var canPlace = true;
            for (var i = 0; i < token.Length; i++)
            {
                var cellIndex = (row * session.MatrixWidth) + startColumn + i;
                if (occupied[cellIndex])
                {
                    canPlace = false;
                    break;
                }
            }

            if (!canPlace)
                continue;

            for (var i = 0; i < token.Length; i++)
            {
                var cellIndex = (row * session.MatrixWidth) + startColumn + i;
                occupied[cellIndex] = true;
                indices.Add(cellIndex);

                var cell = session.MatrixCells[cellIndex];
                cell.Glyph = token[i];
                cell.Word = token;
            }

            return true;
        }

        return false;
    }

    private List<string> GenerateBackdoorTokens(int count)
    {
        var result = new List<string>(count);
        var pairs = new[] { ('(', ')'), ('[', ']'), ('{', '}'), ('<', '>') };

        for (var i = 0; i < count; i++)
        {
            var pair = _random.Pick(pairs);
            var innerLength = _random.Next(3, 7);
            var inner = new char[innerLength];
            for (var j = 0; j < inner.Length; j++)
                inner[j] = JunkSymbols[_random.Next(JunkSymbols.Length)];

            result.Add($"{pair.Item1}{new string(inner)}{pair.Item2}");
        }

        return result;
    }

    private void UpdateUi(EntityUid uid, DecryptionTerminalComponent component, List<string>? overrideLog = null)
    {
        var hasCarrier = TryGetCarrier(uid, out var carrier);
        _sessions.TryGetValue(uid, out var session);

        var protocol = session?.ProtocolTitle ?? "DATA DECRYPTION TERMINAL";

        var tierLabel = "N/A";
        if (carrier is { } c && TryComp<DecryptionTechnologyComponent>(c, out var technology))
            tierLabel = technology.Tier.ToString();

        var matrixWidth = session?.MatrixWidth ?? component.MatrixWidth;
        var matrixHeight = session?.MatrixHeight ?? component.MatrixHeight;
        var matrix = new List<DecryptionMatrixCellData>();
        var log = overrideLog ?? new List<string>();

        var attemptsRemaining = session?.AttemptsRemaining ?? component.MaxAttempts;
        var maxAttempts = session?.MaxAttempts ?? component.MaxAttempts;
        var integrity = session?.Integrity ?? BaseIntegrity;
        var timeRemaining = session?.TimerActive == true ? Math.Max(0, (int) MathF.Ceiling(session.TimerRemaining)) : -1;

        if (session != null)
        {
            matrix = session.MatrixCells
                .Select(cell => new DecryptionMatrixCellData(cell.Glyph, cell.Word, cell.BackdoorId))
                .ToList();

            log = session.Log.ToList();
            tierLabel = session.Tier.ToString();
        }
        else if (log.Count == 0)
        {
            if (!hasCarrier)
            {
                log.Add("> INSERT TECHNOLOGY PAYLOAD INTO SLOT.");
            }
            else
            {
                log.Add("> TERMINAL READY.");
                log.Add("> PRESS START.");
            }
        }

        var state = new DecryptionBoundUiState(
            protocol,
            hasCarrier,
            session != null,
            attemptsRemaining,
            maxAttempts,
            integrity,
            tierLabel,
            timeRemaining,
            matrixWidth,
            matrixHeight,
            matrix,
            log
        );

        _ui.SetUiState(uid, DecryptionUiKey.Key, state);
    }

    private static int CountExactPositionMatches(string guessed, string password)
    {
        var len = Math.Min(guessed.Length, password.Length);
        var matches = 0;
        for (var i = 0; i < len; i++)
        {
            if (guessed[i] == password[i])
                matches++;
        }

        return matches;
    }

    private static void TrimLog(List<string> log, int maxLines)
    {
        var limit = Math.Max(1, maxLines);
        if (log.Count <= limit)
            return;

        var trim = log.Count - limit;
        log.RemoveRange(0, trim);
    }
}



