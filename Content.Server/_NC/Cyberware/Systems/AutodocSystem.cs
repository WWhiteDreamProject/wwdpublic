using Content.Shared._NC.Cyberware;
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware.UI;
using Robust.Server.GameObjects;
using Content.Shared.DoAfter;
using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Content.Shared.Verbs;

namespace Content.Server._NC.Cyberware.Systems;

/// <summary>
///     Серверная система Автодока.
///     Сканирует импланты рядом, управляет UI и запускает DoAfter для установки/извлечения.
/// </summary>
public sealed class AutodocSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly CyberwareSystem _cyberware = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <summary>Радиус сканирования имплантов вокруг автодока (в тайлах).</summary>
    private const float ScanRange = 2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareAutodocComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CyberwareAutodocComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<CyberwareAutodocComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<CyberwareAutodocComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        SubscribeLocalEvent<CyberwareAutodocComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CyberwareAutodocComponent, AutodocInstallBuiMsg>(OnInstallMessage);
        SubscribeLocalEvent<CyberwareAutodocComponent, AutodocRemoveBuiMsg>(OnRemoveMessage);

        SubscribeLocalEvent<CyberwareAutodocComponent, AutodocInstallDoAfterEvent>(OnInstallDoAfter);
        SubscribeLocalEvent<CyberwareAutodocComponent, AutodocRemoveDoAfterEvent>(OnRemoveDoAfter);
    }

    private void OnInit(EntityUid uid, CyberwareAutodocComponent component, ComponentInit args)
    {
        component.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, CyberwareAutodocComponent.BodyContainerId);
    }

    private void OnCanDrop(EntityUid uid, CyberwareAutodocComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = HasComp<CyberwareComponent>(args.Dragged) && component.BodyContainer.ContainedEntity == null;
        args.Handled = true;
    }

    private void OnDragDrop(EntityUid uid, CyberwareAutodocComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled || component.BodyContainer.ContainedEntity != null)
            return;

        if (_container.Insert(args.Dragged, component.BodyContainer))
        {
            _standing.Stand(args.Dragged, force: true);
            args.Handled = true;
            UpdateUI(uid);
        }
    }

    private void OnGetVerbs(EntityUid uid, CyberwareAutodocComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.BodyContainer.ContainedEntity == null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Извлечь пациента",
            Category = VerbCategory.Eject,
            Priority = 1,
            Act = () =>
            {
                var patient = component.BodyContainer.ContainedEntity;
                if (patient != null)
                {
                    _container.Remove(patient.Value, component.BodyContainer);
                    _standing.Down(patient.Value, true);
                    UpdateUI(uid);
                }
            }
        });
    }

    private void OnUIOpened(EntityUid uid, CyberwareAutodocComponent component, BoundUIOpenedEvent args)
    {
        UpdateUI(uid);
    }

    private EntityUid? GetPatient(EntityUid autodoc)
    {
        if (TryComp<CyberwareAutodocComponent>(autodoc, out var component))
            return component.BodyContainer.ContainedEntity;
        return null;
    }

    /// <summary>
    ///     Обновляет BUI: установленные импланты + сканирует доступные рядом.
    /// </summary>
    public void UpdateUI(EntityUid uid)
    {
        var patient = GetPatient(uid);
        var installedNet = new Dictionary<CyberwareSlot, NetEntity>();
        var available = new List<AvailableImplantData>();

        // Собираем установленные импланты пациента
        if (patient != null && TryComp<CyberwareComponent>(patient.Value, out var cyberware))
        {
            foreach (var (slot, implantUid) in cyberware.InstalledImplants)
                installedNet[slot] = GetNetEntity(implantUid);
        }

        // Сканируем импланты в радиусе ScanRange от автодока
        var coords = Transform(uid).Coordinates;
        foreach (var nearby in _lookup.GetEntitiesInRange(coords, ScanRange, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (!TryComp<CyberwareImplantComponent>(nearby, out var impComp))
                continue;

            if (impComp.Category == CyberwareCategory.None)
                continue;

            var name = MetaData(nearby).EntityName;
            available.Add(new AvailableImplantData(
                GetNetEntity(nearby), name, impComp.Category, impComp.HumanityCost));
        }

        var state = new AutodocBoundUserInterfaceState(
            patient != null ? GetNetEntity(patient.Value) : null,
            installedNet,
            available);

        _ui.SetUiState(uid, CyberwareAutodocUiKey.Key, state);
    }

    private void OnInstallMessage(EntityUid uid, CyberwareAutodocComponent component, AutodocInstallBuiMsg args)
    {
        var patient = GetPatient(uid);
        if (patient == null) return;

        var user = args.Actor;
        var implantUid = GetEntity(args.ImplantEntity);

        // Проверяем что имплант существует
        if (!TryComp<CyberwareImplantComponent>(implantUid, out _))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user,
            TimeSpan.FromSeconds(component.InstallTime),
            new AutodocInstallDoAfterEvent(args.ImplantEntity, args.Slot),
            uid, target: patient.Value, used: implantUid)
        {
            BreakOnMove = true,
            NeedHand = false, // Имплант берётся из окружения, не из рук
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnRemoveMessage(EntityUid uid, CyberwareAutodocComponent component, AutodocRemoveBuiMsg args)
    {
        var patient = GetPatient(uid);
        if (patient == null) return;

        var user = args.Actor;

        var doAfterArgs = new DoAfterArgs(EntityManager, user,
            TimeSpan.FromSeconds(component.RemoveTime),
            new AutodocRemoveDoAfterEvent(args.Slot),
            uid, target: patient.Value)
        {
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnInstallDoAfter(EntityUid uid, CyberwareAutodocComponent component, AutodocInstallDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        var patient = args.Args.Target.Value;
        var implant = args.Args.Used.Value;

        if (_cyberware.TryInstallImplant(patient, implant))
            UpdateUI(uid);

        args.Handled = true;
    }

    private void OnRemoveDoAfter(EntityUid uid, CyberwareAutodocComponent component, AutodocRemoveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        var patient = args.Args.Target.Value;

        if (_cyberware.TryRemoveImplant(patient, args.Slot))
            UpdateUI(uid);

        args.Handled = true;
    }
}