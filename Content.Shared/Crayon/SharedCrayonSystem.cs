using Content.Shared._White.Hands.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System;
using System.Numerics;

namespace Content.Shared.Crayon;

// WWDP EDIT START - LITERALLY THE ENTIRE FILE FROM THIS POINT ONWARDS - THIS SYSTEM USED TO BE EMPTY - WTF?
[Virtual]
public abstract class SharedCrayonSystem : EntitySystem
{
    [Dependency] protected readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MouseWheelUp, new PointerInputCmdHandler(MouseWheelUp))
            .Bind(ContentKeyFunctions.MouseWheelDown, new PointerInputCmdHandler(MouseWheelDown))
            .Register<SharedCrayonSystem>();

        SubscribeLocalEvent<CrayonComponent, HandDeselectedEvent>(OnCrayonHandDeselected);
        SubscribeLocalEvent<CrayonComponent, HandSelectedEvent>(OnCrayonHandSelected);
        SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonBoundUIColor);
        SubscribeLocalEvent<CrayonComponent, DroppedEvent>(OnCrayonDropped);
        SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonBoundUI);
    }

    private void OnCrayonBoundUI(EntityUid uid, CrayonComponent component, CrayonSelectMessage args)
    {
        // Check if the selected state is valid
        if (!_prototypeManager.TryIndex<DecalPrototype>(args.State, out var prototype) || !prototype.Tags.Contains("crayon"))
            return;

        component.SelectedState = args.State;
        Dirty(uid, component);
    }

    private void OnCrayonBoundUIColor(EntityUid uid, CrayonComponent component, CrayonColorMessage args)
    {
        // you still need to ensure that the given color is a valid color
        if (!component.SelectableColor || args.Color == component.Color)
            return;

        component.Color = args.Color;
        Dirty(uid, component);
    }

    private void OnCrayonDropped(EntityUid uid, CrayonComponent component, DroppedEvent args)
    {
        // TODO: Use the existing event.
        _uiSystem.CloseUi(uid, CrayonComponent.CrayonUiKey.Key, args.User); // WWDP EDIT
    }

    private readonly Angle _RotationIncrement = Angle.FromDegrees(5);
    private bool MouseWheelUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not EntityUid playerUid ||
            HasComp<HoldingDropComponent>(playerUid) ||
            !TryComp<HandsComponent>(playerUid, out var hands))
            return false;

        EntityUid? crayonUid = _hands.GetActiveItem(playerUid);
        if (!TryComp<CrayonComponent>(crayonUid, out var crayonComp))
            return false;

        crayonComp.Angle += _RotationIncrement;
        Dirty(crayonUid.Value, crayonComp);
        return false;
    }

    private bool MouseWheelDown(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not EntityUid playerUid ||
            HasComp<HoldingDropComponent>(playerUid) ||
            !TryComp<HandsComponent>(playerUid, out var hands))
            return false;

        EntityUid? crayonUid = _hands.GetActiveItem(playerUid);
        if (!TryComp<CrayonComponent>(crayonUid, out var crayonComp))
            return false;

        crayonComp.Angle -= _RotationIncrement;
        Dirty(crayonUid.Value, crayonComp);
        return false;
    }

    protected virtual void OnCrayonHandSelected(EntityUid uid, CrayonComponent comp, HandSelectedEvent args)
    {
        comp.Angle = 0;
        Dirty(uid, comp);
    }

    protected virtual void OnCrayonHandDeselected(EntityUid uid, CrayonComponent comp, HandDeselectedEvent args)
    {
        comp.Angle = 0;
        Dirty(uid, comp);
    }

}

// WWDP EDIT END
