using Content.Shared.Actions;
using Content.Shared.Aliens.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Aliens.Systems;

public sealed class AcidMakerSystem : EntitySystem
{
    // Managers
    [Dependency] private readonly INetManager _netManager = default!;

    // Systems
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPlasmaVesselSystem _plasmaVesselSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AcidMakerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AcidMakerComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<AcidMakerComponent, AcidMakeActionEvent>(OnAcidMakingStart);
    }

    /// <summary>
    /// Giveths the action to preform making acid on the entity
    /// </summary>
    private void OnMapInit(EntityUid uid, AcidMakerComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.ActionEntity, comp.Action);
    }

    /// <summary>
    /// Takeths away the action to preform making acid from the entity.
    /// </summary>
    private void OnCompRemove(EntityUid uid, AcidMakerComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnAcidMakingStart(EntityUid uid, AcidMakerComponent comp, AcidMakeActionEvent args)
    {
        if (!_hands.TryGetEmptyHand(uid, out _))
            return;

        if (TryComp<PlasmaVesselComponent>(uid, out var plasmaComp)
        && plasmaComp.Plasma < comp.PlasmaCost)
        {
            _popupSystem.PopupClient(Loc.GetString(comp.PopupText), uid, uid);
            return;
        }

        if (_netManager.IsClient) // Have to do this because spawning stuff in shared is CBT.
            return;

        var newEntity = Spawn(comp.EntityProduced, Transform(uid).Coordinates);

        _stackSystem.TryMergeToHands(newEntity, uid);
        args.Handled = true;

        _plasmaVesselSystem.ChangePlasmaAmount(uid, -comp.PlasmaCost);

    }
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class AcidMakeActionEvent : InstantActionEvent { }

