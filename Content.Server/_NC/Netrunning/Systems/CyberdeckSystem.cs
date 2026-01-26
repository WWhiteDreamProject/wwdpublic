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
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!; // Added dependency

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
        SubscribeLocalEvent<CyberdeckComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CyberdeckComponent, CyberdeckQuickhackDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CyberdeckComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnDoAfter(EntityUid uid, CyberdeckComponent component, CyberdeckQuickhackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var target = GetEntity(args.TargetId);
        var programUid = GetEntity(args.ProgramId);

        if (!TryComp<NetProgramComponent>(programUid, out var program))
        {
            return;
        }

        _quickhack.ApplyEffect(target, program);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, CyberdeckComponent component, AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        var target = args.Target.Value;

        // Проверяем: живое существо (MobState) ИЛИ киборг
        var isMob = HasComp<MobStateComponent>(target);
        var isBorg = HasComp<BorgChassisComponent>(target);

        if (!isMob && !isBorg)
            return;

        // Проверяем дистанцию от ПОЛЬЗОВАТЕЛЯ до цели и прямую видимость (используем args.User)
        if (!_interaction.InRangeUnobstructed(args.User, target, component.Range))
            return;

        component.ActiveTarget = args.Target;
        Dirty(uid, component);
        UpdateUi(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, CyberdeckComponent component, InteractUsingEvent args)
    {
        // Проверяем, что используется программа
        if (!HasComp<NetProgramComponent>(args.Used))
            return;

        // Получаем все слоты кибердека
        if (!TryComp<ItemSlotsComponent>(uid, out var slotsComp))
            return;

        // Ищем первый свободный слот среди всех program_slot_*
        foreach (var (slotName, slot) in slotsComp.Slots)
        {
            // Пропускаем слоты, которые не являются слотами программ
            if (!slotName.StartsWith("program_slot_"))
                continue;

            if (slot.Item == null)
            {
                // Нашли свободный слот - вставляем
                if (_itemSlots.TryInsert(uid, slotName, args.Used, args.User))
                {
                    args.Handled = true;
                    UpdateUi(uid, component);
                    return;
                }
            }
        }

        // Все слоты заняты - показываем попап
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
        var programUid = GetEntity(args.ProgramId);
        if (!TryComp<NetProgramComponent>(programUid, out var program))
            return;

        // Получаем пользователя (кто держит кибердек)
        if (!TryComp<TransformComponent>(uid, out var xform) || xform.ParentUid == EntityUid.Invalid)
            return;

        var user = xform.ParentUid;

        // If Quickhack, validate target
        if (program.ProgramType == NetProgramType.Quickhack)
        {
            if (component.ActiveTarget == null)
                return;

            var target = component.ActiveTarget.Value;

            // Проверка дистанции от ПОЛЬЗОВАТЕЛЯ до цели
            if (Deleted(target) || !_interaction.InRangeUnobstructed(user, target, component.Range))
            {
                // Popup: Target out of range
                component.ActiveTarget = null;
                Dirty(uid, component);
                UpdateUi(uid, component);
                return;
            }

            if (!TryUseRam(uid, program.RamCost, component))
                return;

            // Start DoAfter на пользователе
            var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(program.UploadTime), new CyberdeckQuickhackDoAfterEvent(GetNetEntity(target), GetNetEntity(programUid)), uid, target: target, used: uid)
            {
                BreakOnMove = false,
                BreakOnDamage = true,
                NeedHand = true,
                DistanceThreshold = component.Range // Явно задаем дистанцию
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
        // Battery Check
        if (TryComp<BatteryComponent>(uid, out var battery) && battery.CurrentCharge <= 0)
            return; // No power, UI won't open

        _uiSystem.OpenUi(uid, CyberdeckUiKey.Key, args.User);
    }

    private void OnUnwielded(EntityUid uid, CyberdeckComponent component, ref ItemUnwieldedEvent args)
    {
        _uiSystem.CloseUi(uid, CyberdeckUiKey.Key, args.User);
    }

    private void AddOpenUiVerb(EntityUid uid, CyberdeckComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
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

            // Only regenerate if battery has charge
            if (battery.CurrentCharge <= 0)
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

    private void UpdateUi(EntityUid uid, CyberdeckComponent deck)
    {
        var programs = new Dictionary<NetEntity, NetProgramData>();

        // Scan all containers for programs
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

        _uiSystem.SetUiState(uid, CyberdeckUiKey.Key, new CyberdeckBoundUiState(deck.CurrentRam, deck.MaxRam, programs, GetNetEntity(deck.ActiveTarget), deck.ActiveTarget != null ? Name(deck.ActiveTarget.Value) : null));
    }
}
