using Content.Server.Power.Components;
using Content.Shared._NC.Netrunning.Components;
using Content.Shared._NC.Netrunning; // Added import
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Content.Shared.Interaction;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Timing;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Popups;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Player;

namespace Content.Server._NC.Netrunning.Systems;

public sealed partial class CyberdeckSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedWieldableSystem _wieldable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!; // Added

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberdeckComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<CyberdeckComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<CyberdeckComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
        SubscribeLocalEvent<CyberdeckComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberdeckComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<CyberdeckComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<CyberdeckComponent, CyberdeckProgramRequestMessage>(OnProgramRequest);
        SubscribeLocalEvent<CyberdeckComponent, CyberdeckSetTargetMessage>(OnSetTarget);

        SubscribeLocalEvent<CyberdeckComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CyberdeckComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CyberdeckComponent, UseInHandEvent>(OnUseInHand);
    }

    [Dependency] private readonly NetServerSystem _netServer = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HackingSystem _hacking = default!;

    private void OnUseInHand(EntityUid uid, CyberdeckComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // Custom Wielding Logic:
        // If NOT wielded -> Try to Wield.
        // If Wielded -> Toggle UI.

        var isWielded = TryComp<WieldableComponent>(uid, out var w) && w.Wielded;

        // Debug
        //_popup.PopupEntity($"UseInHand: Wielded={isWielded}", uid, args.User);

        if (!isWielded && w != null)
        {
            if (_wieldable.TryWield(uid, w, args.User))
            {
                // _popup.PopupEntity("Wield Success", uid, args.User);
            }
            else
            {
                _popup.PopupEntity("Failed to wield (Need 2 hands free?)", uid, args.User);
            }
        }
        else
        {
            _uiSystem.TryToggleUi(uid, CyberdeckUiKey.Key, args.User);
        }

        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, CyberdeckComponent component, AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        // Strict Wield Check
        var isWielded = TryComp<WieldableComponent>(uid, out var w) && w.Wielded;
        if (!isWielded)
        {
            _popup.PopupEntity("You must hold the deck with both hands!", uid, args.User);
            return;
        }

        var target = args.Target.Value;

        // Check Range
        if (!_interaction.InRangeUnobstructed(args.User, target, component.Range))
            return;

        component.ActiveTarget = args.Target;
        Dirty(uid, component);
        UpdateUi(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, CyberdeckComponent component, InteractUsingEvent args)
    {
        if (!HasComp<NetProgramComponent>(args.Used))
            return;

        if (!TryComp<ItemSlotsComponent>(uid, out var slotsComp))
            return;

        foreach (var (slotName, slot) in slotsComp.Slots)
        {
            if (!slotName.StartsWith("program_slot_"))
                continue;

            if (slot.Item == null)
            {
                if (_itemSlots.TryInsert(uid, slotName, args.Used, args.User))
                {
                    args.Handled = true;
                    UpdateUi(uid, component);
                    return;
                }
            }
        }

        _popup.PopupEntity("Все слоты для программ заняты!", uid, args.User);
        args.Handled = true;
    }

    private void OnContainerModified(EntityUid uid, CyberdeckComponent component, ContainerModifiedMessage args)
    {
        UpdateUi(uid, component);
    }

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly QuickhackSystem _quickhack = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private void OnProgramRequest(EntityUid uid, CyberdeckComponent component, CyberdeckProgramRequestMessage args)
    {
        // Check Wielded
        var isWielded = TryComp<WieldableComponent>(uid, out var w) && w.Wielded;
        if (!isWielded)
            return;

        var programUid = GetEntity(args.ProgramId);
        if (!TryComp<NetProgramComponent>(programUid, out var program))
            return;

        if (!TryComp<TransformComponent>(uid, out var xform) || xform.ParentUid == EntityUid.Invalid)
            return;

        var user = xform.ParentUid;

        if (program.ProgramType == NetProgramType.Quickhack)
        {
            if (component.ActiveTarget == null)
                return;

            var target = component.ActiveTarget.Value;

            if (Deleted(target) || !_interaction.InRangeUnobstructed(user, target, component.Range))
            {
                component.ActiveTarget = null;
                Dirty(uid, component);
                UpdateUi(uid, component);
                return;
            }

            if (!TryUseRam(uid, program.RamCost, component))
                return;

            var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(program.UploadTime), new CyberdeckQuickhackDoAfterEvent(GetNetEntity(target), GetNetEntity(programUid)), uid, target: target, used: uid)
            {
                BreakOnMove = false,
                BreakOnDamage = true,
                NeedHand = false, // Wielded requires both hands, so "NeedHand" might fail if it checks for free hand. Better to use false? Wielding occupies hands.
                DistanceThreshold = component.Range
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
        }
    }

    private void OnStartup(EntityUid uid, CyberdeckComponent component, ComponentStartup args)
    {
        UpdateUi(uid, component);
    }

    private void OnWielded(EntityUid uid, CyberdeckComponent component, ref ItemWieldedEvent args)
    {
        if (TryComp<BatteryComponent>(uid, out var battery) && battery.CurrentCharge <= 0)
            return;

        _uiSystem.OpenUi(uid, CyberdeckUiKey.Key, args.User);
    }

    private void OnUnwielded(EntityUid uid, CyberdeckComponent component, ref ItemUnwieldedEvent args)
    {
        _uiSystem.CloseUi(uid, CyberdeckUiKey.Key, args.User);
    }

    private void AddOpenUiVerb(EntityUid uid, CyberdeckComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        var isWielded = TryComp<WieldableComponent>(uid, out var w) && w.Wielded;
        if (!args.CanAccess || !args.CanInteract || !isWielded)
            return;

        args.Verbs.Add(new ActivationVerb
        {
            Text = "Open Cyberdeck",
            Act = () => _uiSystem.TryToggleUi(uid, CyberdeckUiKey.Key, args.User)
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CyberdeckComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var deck, out var battery))
        {
            if (deck.CurrentRam >= deck.MaxRam)
                continue;

            if (battery.CurrentCharge <= 0)
                continue;

            // Block RAM recovery during active hacking session
            if (_hacking.IsHacking(uid))
                continue;

            deck.RecoveryAccumulator += frameTime * deck.RecoverySpeed;
            if (deck.RecoveryAccumulator >= 1.0f)
            {
                var amount = (int) deck.RecoveryAccumulator;
                deck.RecoveryAccumulator -= amount;

                deck.CurrentRam = System.Math.Min(deck.CurrentRam + amount, deck.MaxRam);

                Dirty(uid, deck);
                UpdateUi(uid, deck);
            }
        }
    }

    public bool TryUseRam(EntityUid uid, int cost, CyberdeckComponent? deck = null)
    {
        if (!Resolve(uid, ref deck))
            return false;

        if (deck.CurrentRam >= cost)
        {
            deck.CurrentRam -= cost;
            Dirty(uid, deck);
            UpdateUi(uid, deck);
            return true;
        }

        return false;
    }

    private void OnSetTarget(EntityUid uid, CyberdeckComponent component, CyberdeckSetTargetMessage args)
    {
        var user = args.Actor;

        // Enforce Wielded
        var isWielded = TryComp<WieldableComponent>(uid, out var w) && w.Wielded;
        if (!isWielded)
            return;

        var target = GetEntity(args.TargetId);

        if (Deleted(target) || !_interaction.InRangeUnobstructed(user, target, component.Range))
            return;

        component.ActiveTarget = target;
        Dirty(uid, component);
        UpdateUi(uid, component);
    }



    public void UpdateUi(EntityUid uid, CyberdeckComponent deck)
    {
        var programs = new Dictionary<NetEntity, NetProgramData>();

        foreach (var container in _containerSystem.GetAllContainers(uid))
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp<NetProgramComponent>(entity, out var program))
                {
                    programs[GetNetEntity(entity)] = new NetProgramData(program.ProgramType, program.RamCost, program.EnergyCost);
                }
            }
        }

        // Return the Cached Scan results
        _uiSystem.SetUiState(uid, CyberdeckUiKey.Key, new CyberdeckBoundUiState(deck.CurrentRam, deck.MaxRam, programs, GetNetEntity(deck.ActiveTarget), deck.ActiveTarget != null ? Name(deck.ActiveTarget.Value) : null, deck.LastScan));
    }
}
