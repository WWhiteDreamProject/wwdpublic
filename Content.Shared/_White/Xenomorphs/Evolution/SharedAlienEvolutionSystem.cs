using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.RadialSelector;
using Content.Shared.Standing;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._White.Xenomorphs.Evolution;

public abstract class SharedAlienEvolutionSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlienEvolutionComponent, MapInitEvent>(OnAlienEvolutionMapInit);
        SubscribeLocalEvent<AlienEvolutionComponent, ComponentShutdown>(OnAlienEvolutionShutdown);
        SubscribeLocalEvent<AlienEvolutionComponent, OpenEvolutionsActionEvent>(OnOpenEvolutionsAction);
        SubscribeLocalEvent<AlienEvolutionComponent, RadialSelectorSelectedMessage>(OnEvolutionRecieved);
        SubscribeLocalEvent<AlienEvolutionComponent, AlienEvolutionDoAfterEvent>(OnAlienEvolutionDoAfter);
    }

    private void OnAlienEvolutionMapInit(EntityUid uid, AlienEvolutionComponent component, ref MapInitEvent args) =>
        _actions.AddAction(uid, ref component.EvolutionAction, component.EvolutionActionId);

    private void OnAlienEvolutionShutdown(EntityUid uid, AlienEvolutionComponent component, ComponentShutdown args) =>
        _actions.RemoveAction(uid, component.EvolutionAction);

    private void OnOpenEvolutionsAction(EntityUid uid, AlienEvolutionComponent component, ref OpenEvolutionsActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _ui.TryToggleUi(uid, RadialSelectorUiKey.Key, uid);
        _ui.SetUiState(uid, RadialSelectorUiKey.Key, new RadialSelectorState(component.EvolvesTo, true));
    }

    private void OnEvolutionRecieved(EntityUid uid, AlienEvolutionComponent component, RadialSelectorSelectedMessage args)
    {
        if (component.Points < component.Max)
        {
            Popup.PopupClient(Loc.GetString("xenomorphs-evolution-not-enough-points", ("seconds", (component.Max - component.Points) * component.PointsPerSecond)), uid, uid);
            return;
        }

        var actor = args.Actor;
        _ui.CloseUi(uid, RadialSelectorUiKey.Key, actor);

        var ev = new AlienEvolutionDoAfterEvent(args.SelectedItem);
        var doAfter = new DoAfterArgs(EntityManager, uid, component.EvolutionDelay, ev, uid);

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _jitter.DoJitter(uid, component.EvolutionDelay, true, 80, 8, true);

        var popupOthers = Loc.GetString("xenomorphs-evolution-start-others", ("uid", uid));
        Popup.PopupEntity(popupOthers, uid, Filter.PvsExcept(uid), true, PopupType.Medium);

        var popupSelf = Loc.GetString("xenomorphs-evolution-start-self");
        Popup.PopupClient(popupSelf, uid, uid, PopupType.Medium);
    }

    private void OnAlienEvolutionDoAfter(EntityUid uid, AlienEvolutionComponent component, ref AlienEvolutionDoAfterEvent args)
    {
        if (_net.IsClient || args.Handled || args.Cancelled || !_mind.TryGetMind(uid, out var mindId, out _))
            return;

        args.Handled = true;

        var coordinates = _transform.GetMoverCoordinates(uid);
        var newXeno = Spawn(args.Choice, coordinates);

        _mind.TransferTo(mindId, newXeno);
        _mind.UnVisit(mindId);

        RaiseLocalEvent(uid, new DropHandItemsEvent());

        _adminLog.Add(LogType.Mind, $"{ToPrettyString(uid)} evolved into {ToPrettyString(newXeno)}");

        Del(uid);

        Popup.PopupEntity(Loc.GetString("xenomorphs-evolution-end"), newXeno, newXeno);
    }
}
