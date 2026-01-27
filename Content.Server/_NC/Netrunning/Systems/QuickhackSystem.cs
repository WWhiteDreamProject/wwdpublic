using Content.Shared._NC.Netrunning.Components;
using Content.Shared._NC.Netrunning;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Stunnable;
using Content.Shared.Popups; // Added

using Content.Server.Electrocution;
using Content.Server.Power.Components;

namespace Content.Server._NC.Netrunning.Systems;

public sealed class QuickhackSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly NetServerSystem _netServer = default!;
    [Dependency] private readonly HackingSystem _hacking = default!;
    [Dependency] private readonly Content.Shared.Containers.ItemSlots.ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly CyberdeckSystem _cyberdeck = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CyberdeckComponent, CyberdeckQuickhackDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, CyberdeckComponent component, CyberdeckQuickhackDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var target = GetEntity(args.TargetId);

        // Get program first to check if it's HighJack
        if (!TryComp<NetProgramComponent>(GetEntity(args.ProgramId), out var program))
            return;

        // Protection Check - BUT skip for HighJack (it's the entry point to hacking)
        var protectingServer = _netServer.GetProtectingServer(target);
        if (protectingServer != null && _netServer.HasActiveIce(protectingServer.Value)
            && program.QuickhackType != QuickhackType.HighJack)
        {
            // Intercept: Start Hacking Session
            _hacking.StartHacking(args.User, uid, target, protectingServer.Value);
            return; // Cancel Quickhack effect
        }

        // Apply Effect
        ApplyEffect(uid, args.User, target, program);
    }

    public void ApplyEffect(EntityUid deckUid, EntityUid user, EntityUid target, NetProgramComponent program)
    {
        switch (program.QuickhackType)
        {
            case QuickhackType.WeaponDrop:
                _hands.TryDrop(target);
                break;

            case QuickhackType.Damage:
                var damageSpec = new DamageSpecifier();
                var damageType = "Shock";

                // Validate if Shock exists, otherwise fallback to Blunt
                if (!_proto.HasIndex<DamageTypePrototype>(damageType))
                    damageType = "Blunt";

                if (_proto.HasIndex<DamageTypePrototype>(damageType))
                {
                    damageSpec.DamageDict.Add(damageType, program.Damage);
                    _damageable.TryChangeDamage(target, damageSpec);
                }

                // Electrocution if stunDuration is set
                if (program.StunDuration > 0)
                {
                    // TryDoElectrocution(uid, sourceUid, damage, duration, refresh, ignoreInsulation)
                    _electrocution.TryDoElectrocution(target, null, program.Damage, TimeSpan.FromSeconds(program.StunDuration), true, ignoreInsulation: true);
                }
                break;

            case QuickhackType.Blind:
                EnsureComp<BlindableComponent>(target);
                _status.TryAddStatusEffect(target, "TemporaryBlindness", TimeSpan.FromSeconds(program.Duration), true, "TemporaryBlindness");
                break;

            case QuickhackType.Ping:
                var status = "Unprotected";
                // GetProtectingServer, GetActiveIceName etc are methods of NetServerSystem or HackingSystem?
                // Previously HackingSystem was used. Checking HackingSystem source for GetActiveIceName?
                // HackingSystem has GetActiveIce(session). It doesn't seem to expose a generic "GetActiveIceName(server)" that is public static or simple.
                // However, NetServerSystem has GetProtectingServer.

                if (_netServer.GetProtectingServer(target) is { } server)
                {
                    status = $"Protected by {Name(server)}";
                    if (_netServer.HasActiveIce(server))
                    {
                        status += " (ICE Active)";

                        // Safe access to item slots
                        if (_itemSlots.GetItemOrNull(server, "ice_slot_1") is { } iceUid)
                        {
                            status += $" - Detected {Name(iceUid)}";
                        }
                    }
                    else
                    {
                        status += " (No Active ICE)";
                    }
                }
                _popup.PopupEntity($"Ping Result: {Name(target)} - {status}", target, user);
                break;

            case QuickhackType.HighJack:
                // HighJack: Entry Point to Network Architecture (Tower)
                // Find protecting server using ProtectedByComponent (or target itself if it's a server)
                EntityUid? serverToHack = null;

                if (HasComp<NetServerComponent>(target))
                {
                    serverToHack = target;
                }
                else if (TryComp<ProtectedByComponent>(target, out var protection) && protection.Server.HasValue)
                {
                    serverToHack = protection.Server.Value;
                }

                if (serverToHack == null)
                {
                    _popup.PopupEntity("Target is not connected to any network.", deckUid, user);
                    break;
                }

                // Start Hacking Session (Tower Mini-game)
                _hacking.StartHacking(user, deckUid, target, serverToHack.Value);
                break;
        }
    }
}
