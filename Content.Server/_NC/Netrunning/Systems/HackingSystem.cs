using Content.Shared._NC.Netrunning.Components;
using Content.Shared._NC.Netrunning.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using Robust.Shared.Audio.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server._NC.Netrunning.Systems;

public sealed class HackingSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NetServerSystem _netServer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly Content.Shared.Body.Systems.SharedBodySystem _body = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    // Track active sessions: DeckUID -> Session
    private readonly Dictionary<EntityUid, HackingSession> _sessions = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CyberdeckComponent, HackingUseProgramMessage>(OnUseProgram);
        SubscribeLocalEvent<CyberdeckComponent, HackingPassphraseMessage>(OnPassphrase);
        SubscribeLocalEvent<CyberdeckComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    /// <summary>
    /// Check if a cyberdeck is currently in an active hacking session.
    /// Used to block RAM recovery during hacking.
    /// </summary>
    public bool IsHacking(EntityUid deckUid) => _sessions.ContainsKey(deckUid);

    /// <summary>
    /// Inactivity timeout in seconds before automatic disconnect.
    /// </summary>
    private const float InactivityTimeout = 10f;

    /// <summary>
    /// Tracks when to send UI update (limit to once per second for performance).
    /// </summary>
    private float _uiUpdateAccumulator = 0f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _uiUpdateAccumulator += frameTime;

        // Process timer for each active session
        var toDisconnect = new List<(EntityUid deck, HackingSession session)>();

        foreach (var (deck, session) in _sessions)
        {
            session.InactivityTimer -= frameTime;

            // Decrement popup cooldown
            if (session.PopupCooldown > 0)
                session.PopupCooldown -= frameTime;

            if (session.InactivityTimer <= 0)
            {
                toDisconnect.Add((deck, session));
            }
            else if (_uiUpdateAccumulator >= 1.0f)
            {
                // Update UI every second to show timer
                UpdateHackingState(deck, session);
            }
        }

        if (_uiUpdateAccumulator >= 1.0f)
            _uiUpdateAccumulator = 0f;

        // Disconnect timed out sessions
        foreach (var (deck, session) in toDisconnect)
        {
            ShowPopup(deck, session, Loc.GetString("hacking-session-timeout"), Shared.Popups.PopupType.LargeCaution);
            session.AccumulatedDamage += 15; // Timeout damage
            DisconnectHack(deck, session);
        }
    }

    /// <summary>
    /// Show popup with delay to prevent overlapping messages.
    /// </summary>
    private void ShowPopup(EntityUid deck, HackingSession session, string message, Shared.Popups.PopupType type = Shared.Popups.PopupType.Small)
    {
        // Only show if cooldown expired
        if (session.PopupCooldown <= 0)
        {
            _popup.PopupEntity(message, deck, session.User, type);
            session.PopupCooldown = 0.5f; // 0.5 second delay between popups
        }
    }

    public void StartHacking(EntityUid user, EntityUid deck, EntityUid targetDevice, EntityUid targetServer)
    {
        if (TryComp<CyberdeckComponent>(deck, out var deckComp))
        {
            // Check for Root Access (Admin Key)
            if (deckComp.HackedNetworks.Contains(targetServer))
            {
                // Already hacked! Open Admin Panel directly.
                _popup.PopupEntity(Loc.GetString("hacking-server-already-hacked"), deck, user, Shared.Popups.PopupType.MediumCaution);
                _ui.OpenUi(targetServer, NetServerUiKey.Key, user);
                // Also play a success sound to indicate authorized access
                _audio.PlayPvs("/Audio/Machines/machine_switch.ogg", deck);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user)} accessed already hacked server {ToPrettyString(targetServer)}");
                return;
            }
        }

        // Show generic Hacking Popup
        _popup.PopupEntity(Loc.GetString("hacking-sequence-start"), deck, user);

        // Log
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user)} started hacking {ToPrettyString(targetDevice)} (Server: {ToPrettyString(targetServer)})");

        // 1. Initialize Session
        var session = new HackingSession
        {
            User = user,
            Deck = deck,
            TargetDevice = targetDevice,
            TargetServer = targetServer,
            CurrentIceSlotIndex = 0 // Start at bottom? Or Top? Usually Top (Slot 1 / Index 0)
        };

        _sessions[deck] = session;

        // 2. Open UI
        _ui.OpenUi(deck, HackingUiKey.Key, user);

        // 3. Update Visuals
        if (TryComp<CyberdeckComponent>(deck, out var deckVisuals))
        {
            deckVisuals.ActiveTarget = targetDevice;
            deckVisuals.BeamColor = Color.Blue; // Scanning/Connecting color
            Dirty(deck, deckVisuals);
        }

        // 4. Play Sound
        _audio.PlayPvs("/Audio/Machines/machine_switch.ogg", deck); // Placeholder connect sound

        // 5. Update State
        UpdateHackingState(deck, session);
    }

    private void OnUiClosed(EntityUid uid, CyberdeckComponent component, BoundUIClosedEvent args)
    {
        if (args.UiKey is HackingUiKey)
        {
            // If session still exists, it means it wasn't closed by code (Safe/Success/Fail)
            // Treat as Emergency Disconnect caused by user closing the window manually
            if (_sessions.TryGetValue(uid, out var session))
            {
                // Apply penalty for unsafe disconnect
                session.AccumulatedDamage += 15;
                ShowPopup(uid, session, Loc.GetString("hacking-emergency-disconnect"), Shared.Popups.PopupType.LargeCaution);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(session.User)} manually disconnected from hacking {ToPrettyString(session.TargetServer)}");
                DisconnectHack(uid, session);
            }
        }
    }

    private void OnUseProgram(EntityUid uid, CyberdeckComponent component, HackingUseProgramMessage args)
    {
        if (!_sessions.TryGetValue(uid, out var session)) return;

        // Reset inactivity timer on any action
        session.InactivityTimer = InactivityTimeout;

        var programEnt = GetEntity(args.ProgramEntity);
        if (!TryComp<NetProgramComponent>(programEnt, out var program)) return;

        // Update Scan Beam Color
        if (program.BeamColor != Color.FromHex("#00000000")) // Assuming transparent is default invalid
        {
            component.BeamColor = program.BeamColor;
        }
        else
        {
            // Fallback colors
            if (program.Damage > 0)
                component.BeamColor = Color.Red;
            else if (program.Defense > 0)
                component.BeamColor = Color.Cyan;
            else
                component.BeamColor = Color.Green;
        }
        Dirty(uid, component);

        // Check RAM cost - disconnect with 15 Heat if insufficient
        if (component.CurrentRam < program.RamCost)
        {
            ShowPopup(uid, session, Loc.GetString("hacking-critical-error-ram"), Shared.Popups.PopupType.LargeCaution);
            session.AccumulatedDamage += 15; // Fixed 15 Heat for RAM failure
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(session.User)} disconnected from hacking {ToPrettyString(session.TargetServer)} due to insufficient RAM");
            DisconnectHack(uid, session);
            return;
        }

        // Consume RAM
        component.CurrentRam -= program.RamCost;

        // === CHECK FOR BACKDOOR PROGRAM (Safe Exit) ===
        if (program.Damage == 0 && program.Defense == 0 && program.RamCost <= 10)
        {
            // Assume this is a Backdoor-type program (safe exit)
            // Check if program name contains "Backdoor" via metadata (optional)
            if (TryComp<MetaDataComponent>(programEnt, out var meta) &&
                meta.EntityName.Contains("Backdoor", StringComparison.OrdinalIgnoreCase))
            {
                ShowPopup(uid, session, Loc.GetString("hacking-backdoor-activated"), Shared.Popups.PopupType.Medium);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(session.User)} used a backdoor to safely disconnect from hacking {ToPrettyString(session.TargetServer)}");
                SafeDisconnect(uid, session);
                return;
            }
        }

        // Get Active ICE
        var iceEnt = GetActiveIce(session);
        if (iceEnt == null)
        {
            // No ICE? Success!
            CompleteHack(uid, session);
            return;
        }

        if (!TryComp<NetIceComponent>(iceEnt.Value, out var ice)) return;

        // === CHECK FOR BREACH PROGRAM (Gate-Only) ===
        // Breach programs have very high damage (100+) and only work on Gate ICE
        if (program.Damage >= 100)
        {
            if (TryComp<MetaDataComponent>(programEnt, out var breachMeta) &&
                breachMeta.EntityName.Contains("Breach", StringComparison.OrdinalIgnoreCase))
            {
                if (ice.IceType != NetIceType.Gate)
                {
                    ShowPopup(uid, session, Loc.GetString("hacking-error-breach-gate-only"), Shared.Popups.PopupType.MediumCaution);
                    return;
                }
            }
        }

        // === PLAYER ATTACKS FIRST ===
        if (program.Damage > 0)
        {
            var damageDealt = Math.Max(0, program.Damage);
            ice.CurrentHealth -= damageDealt;

            // Visual feedback
            ShowPopup(uid, session, Loc.GetString("hacking-damage-dealt", ("damage", damageDealt)));
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(session.User)} used {ToPrettyString(programEnt)} dealing {damageDealt} damage to {ToPrettyString(iceEnt.Value)} on {ToPrettyString(session.TargetServer)}");

            if (ice.CurrentHealth <= 0)
            {
                ShowPopup(uid, session, Loc.GetString("hacking-ice-destroyed"), Shared.Popups.PopupType.Large);
                _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(session.User)} destroyed {ToPrettyString(iceEnt.Value)} on {ToPrettyString(session.TargetServer)}");
                session.PasswordAttempts = 0;
                session.CurrentIceSlotIndex++;

                // Delete the ICE entity
                QueueDel(iceEnt.Value);

                var nextIce = GetActiveIce(session);
                if (nextIce == null)
                {
                    CompleteHack(uid, session);
                    return;
                }

                UpdateHackingState(uid, session);
                return; // ICE destroyed, no retaliation
            }
        }

        // === ICE RETALIATES ===
        IceRetaliate(uid, component, session, ice, program.Defense);

        UpdateHackingState(uid, session);
    }

    /// <summary>
    /// Safe disconnect via Backdoor. No accumulated damage applied.
    /// </summary>
    private void SafeDisconnect(EntityUid deck, HackingSession session)
    {
        ShowPopup(deck, session, Loc.GetString("hacking-safe-disconnect"));
        _sessions.Remove(deck);
        _ui.CloseUi(deck, HackingUiKey.Key, session.User);
    }

    /// <summary>
    /// ICE attacks the hacker: reduces RAM, accumulates damage, may disconnect.
    /// </summary>
    private void IceRetaliate(EntityUid deck, CyberdeckComponent deckComp, HackingSession session, NetIceComponent ice, int playerDefense)
    {
        // Calculate RAM damage (reduced by defense)
        int ramDamage = Math.Max(0, ice.RamDamage - playerDefense);
        if (ramDamage > 0)
        {
            deckComp.CurrentRam = Math.Max(0, deckComp.CurrentRam - ramDamage);
            ShowPopup(deck, session, Loc.GetString("hacking-ice-attack", ("ram", ramDamage)), Shared.Popups.PopupType.MediumCaution);
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ice.Owner)} dealt {ramDamage} RAM damage to {ToPrettyString(session.User)} on {ToPrettyString(session.TargetServer)}");
        }
        else
        {
            ShowPopup(deck, session, Loc.GetString("hacking-shield-blocked"));
        }

        // Accumulate physical damage (applied on disconnect)
        session.AccumulatedDamage += ice.Damage;

        // Check disconnect based on ICE type
        switch (ice.IceType)
        {
            case NetIceType.WhiteICE:
                // Sentry has chance to disconnect
                if (_random.Prob(ice.DisconnectChance))
                {
                    ShowPopup(deck, session, Loc.GetString("hacking-sentry-lockout"), Shared.Popups.PopupType.LargeCaution);
                    _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(session.User)} disconnected by SENTRY ICE from {ToPrettyString(session.TargetServer)}");
                    DisconnectHack(deck, session);
                    return;
                }
                break;

            case NetIceType.BlackICE:
                // Killer just deals damage, no disconnect
                break;

            case NetIceType.Gate:
                // Gate doesn't retaliate normally (password-based)
                break;
        }

        // Check if RAM depleted
        if (deckComp.CurrentRam <= 0)
        {
            ShowPopup(deck, session, Loc.GetString("hacking-ram-depleted"), Shared.Popups.PopupType.LargeCaution);
            DisconnectHack(deck, session);
        }
    }

    private void OnPassphrase(EntityUid uid, CyberdeckComponent component, HackingPassphraseMessage args)
    {
        if (!_sessions.TryGetValue(uid, out var session)) return;

        // Reset inactivity timer on password attempt
        session.InactivityTimer = InactivityTimeout;

        var iceEnt = GetActiveIce(session);
        if (iceEnt == null) return;

        if (TryComp<NetIceComponent>(iceEnt.Value, out var ice))
        {
            if (ice.IceType == NetIceType.Gate)
            {
                if (ice.Password == args.Passphrase)
                {
                    ShowPopup(uid, session, Loc.GetString("hacking-access-granted"), Shared.Popups.PopupType.Medium);
                    session.PasswordAttempts = 0; // Reset for next Gate
                    session.CurrentIceSlotIndex++;
                    var nextIce = GetActiveIce(session);
                    if (nextIce == null)
                    {
                        CompleteHack(uid, session);
                        return;
                    }
                }
                else
                {
                    session.PasswordAttempts++;
                    int remaining = ice.MaxPasswordAttempts - session.PasswordAttempts;

                    if (remaining <= 0)
                    {
                        ShowPopup(uid, session, Loc.GetString("hacking-access-denied-lockout"), Shared.Popups.PopupType.LargeCaution);
                        DisconnectHack(uid, session);
                        return;
                    }
                    else
                    {
                        ShowPopup(uid, session, Loc.GetString("hacking-access-denied-remaining", ("remaining", remaining)), Shared.Popups.PopupType.MediumCaution);
                    }
                }
            }
        }
        UpdateHackingState(uid, session);
    }

    /// <summary>
    /// Gets the active ICE for the current floor. Automatically skips empty floors.
    /// </summary>
    private EntityUid? GetActiveIce(HackingSession session)
    {
        // Get slots from server
        if (!TryComp<ItemSlotsComponent>(session.TargetServer, out var slots)) return null;

        // Sort slots to match "Floors" (ice_slot_1 is Floor 1 (Top) -> ice_slot_3 is Floor 3 (Root))
        var sortedSlots = slots.Slots
            .Where(s => s.Key.StartsWith("ice_slot_"))
            .OrderBy(s => s.Key) // 1, 2, 3...
            .ToList();

        // Skip empty floors
        while (session.CurrentIceSlotIndex < sortedSlots.Count)
        {
            var currentSlot = sortedSlots[session.CurrentIceSlotIndex].Value;
            if (currentSlot.Item != null)
                return currentSlot.Item;

            // Empty slot - skip to next floor
            session.CurrentIceSlotIndex++;
        }

        // Passed last floor
        return null;
    }

    private void UpdateHackingState(EntityUid deck, HackingSession session)
    {
        // Build state
        var iceEnt = GetActiveIce(session);
        string iceName = "None";
        int iceHealth = 0;
        int iceMax = 0;
        string iceType = "None";

        if (iceEnt != null && TryComp<NetIceComponent>(iceEnt.Value, out var ice))
        {
            iceName = Name(iceEnt.Value);
            iceHealth = ice.CurrentHealth;
            iceMax = ice.MaxHealth;
            iceType = ice.IceType.ToString();
        }

        // Available Programs from Deck
        var programs = new List<HackingProgramData>();
        if (TryComp<ItemSlotsComponent>(deck, out var deckSlots))
        {
            foreach (var slot in deckSlots.Slots.Values)
            {
                if (slot.Item != null && TryComp<NetProgramComponent>(slot.Item.Value, out var prog) && TryComp<MetaDataComponent>(slot.Item.Value, out var meta))
                {
                    if (prog.ProgramType == NetProgramType.Network)
                    {
                        // Hack: Prototype ID as icon for now, or just generic
                        // Assuming we can pass prototype ID? NetProgramData takes string Icon.
                        programs.Add(new HackingProgramData(GetNetEntity(slot.Item.Value), meta.EntityName, meta.EntityPrototype?.ID ?? "cart", prog.RamCost, prog.Damage, prog.Defense));
                    }
                }
            }
        }

        int ram = 10; // Placeholder
        int maxRam = 10;
        if (TryComp<CyberdeckComponent>(deck, out var deckComp))
        {
            ram = (int) deckComp.CurrentRam;
            maxRam = deckComp.MaxRam;
        }

        var state = new HackingBoundUiState(
            GetNetEntity(session.TargetServer),
            Loc.GetString("hacking-ui-floor", ("floor", session.CurrentIceSlotIndex + 1)),
            iceName,
            iceHealth,
            iceMax,
            iceType,
            ram,
            maxRam,
            session.InactivityTimer,
            session.AccumulatedDamage,
            programs
        );

        _ui.SetUiState(deck, HackingUiKey.Key, state);
    }

    /// <summary>
    /// Disconnect hacker from network (failed hack). Applies accumulated Heat damage.
    /// </summary>
    private void DisconnectHack(EntityUid deck, HackingSession session)
    {
        // Apply accumulated damage as Heat
        if (session.AccumulatedDamage > 0)
        {
            var damageSpec = new DamageSpecifier();
            damageSpec.DamageDict.Add("Heat", session.AccumulatedDamage);

            // Use Shitmed targeting system to hit the Head
            // This attempts to hit the Head, if available.
            // Disable evasion for neural feedback!
            var result = _damageable.TryChangeDamage(
                session.User,
                damageSpec,
                targetPart: Content.Shared._Shitmed.Targeting.TargetBodyPart.Head,
                canEvade: false
            );

            // Fallback: If Head not found (result null) or dealt no damage, try Torso
            if (result == null || result.GetTotal() == 0)
            {
                result = _damageable.TryChangeDamage(
                   session.User,
                   damageSpec,
                   targetPart: Content.Shared._Shitmed.Targeting.TargetBodyPart.Torso,
                   canEvade: false
               );
            }

            // Visuals & Audio for Shock (Always play if we tried to damage)
            Spawn("EffectSparks", Transform(session.User).Coordinates);
            _audio.PlayPvs("/Audio/Effects/sparks4.ogg", session.User);
        }

        _sessions.Remove(deck);
        _ui.CloseUi(deck, HackingUiKey.Key, session.User);

        // Clear visuals
        if (TryComp<CyberdeckComponent>(deck, out var deckComp))
        {
            deckComp.ActiveTarget = null;
            Dirty(deck, deckComp);
        }

        // Play Fail Sound
        _audio.PlayPvs("/Audio/Machines/machine_switch.ogg", deck); // Placeholder fail sound (different pitch?)
    }

    private void CompleteHack(EntityUid deck, HackingSession session)
    {
        ShowPopup(deck, session, Loc.GetString("hacking-system-bypassed"), Shared.Popups.PopupType.Large);
        // No damage on success!
        _sessions.Remove(deck);
        _ui.CloseUi(deck, HackingUiKey.Key, session.User);

        // Clear visuals
        if (TryComp<CyberdeckComponent>(deck, out var deckComp))
        {
            deckComp.ActiveTarget = null;

            // Grant Root Access
            if (deckComp.HackedNetworks.Add(session.TargetServer))
            {
                // New network hacked!
                ShowPopup(deck, session, Loc.GetString("hacking-root-access-saved"), Shared.Popups.PopupType.Large);
            }

            Dirty(deck, deckComp);
        }

        // Play Success Sound
        _audio.PlayPvs("/Audio/Machines/machine_vend.ogg", deck); // Placeholder success sound

        // Open Admin Panel
        _ui.OpenUi(session.TargetServer, NetServerUiKey.Key, session.User);

        // Trigger Success Callback?
        // For now just close. The Quickhack system would need to wait for this result.
        // Or we trigger the Quickhack effect here explicitly if we passed the quickhack info in session.
    }

    private sealed class HackingSession
    {
        public EntityUid User;
        public EntityUid Deck;
        public EntityUid TargetDevice;
        public EntityUid TargetServer;
        public int CurrentIceSlotIndex;

        /// <summary>
        /// Current password attempts for Gate ICE.
        /// </summary>
        public int PasswordAttempts;

        /// <summary>
        /// Accumulated damage from ICE. Applied as Heat on failed hack.
        /// </summary>
        public int AccumulatedDamage;

        /// <summary>
        /// Inactivity timer. Counts down from 10 seconds. Disconnect on 0.
        /// </summary>
        public float InactivityTimer = 10f;

        /// <summary>
        /// Cooldown for popup messages to prevent overlap (seconds).
        /// </summary>
        public float PopupCooldown = 0f;
    }
}
