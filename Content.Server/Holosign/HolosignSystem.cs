using System.Linq;
using Content.Server._White.Holosign;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Storage;

namespace Content.Server.Holosign;

public sealed class HolosignSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!; // WD EDIT

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolosignProjectorComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        SubscribeLocalEvent<HolosignProjectorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HolosignProjectorComponent, UseInHandEvent>(OnUse); // WD EDIT
    }

    private void OnExamine(EntityUid uid, HolosignProjectorComponent component, ExaminedEvent args)
    {
        // WD EDIT START
        var charges = component.Uses;
        var maxCharges = component.MaxUses;
        var activeholo = component.Signs.Count;
        // WD EDIT END

        using (args.PushGroup(nameof(HolosignProjectorComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));
            args.PushMarkup(Loc.GetString("holoprojector-active-holo", ("activeholo", activeholo))); // WD EDIT

            if (charges > 0 && charges == maxCharges)
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
        }
    }

    private void OnBeforeInteract(EntityUid uid, HolosignProjectorComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach // prevent placing out of range
            || HasComp<StorageComponent>(args.Target)) // if it's a storage component like a bag, we ignore usage so it can be stored
            return;

        // WD EDIT START
        if (component.Signs.Contains(args.Target))
        {
            ++component.Uses;
            component.Signs.Remove(args.Target);
            QueueDel(args.Target);
            return;
        }

        if (component.Uses == 0)
        {
            _popup.PopupEntity(Loc.GetString("holoprojector-uses-limit"), args.User, args.User, PopupType.Medium);
            return;
        }
        // WD EDIT END

        // places the holographic sign at the click location, snapped to grid.
        // overlapping of the same holo on one tile remains allowed to allow holofan refreshes
        var holoUid = EntityManager.SpawnEntity(component.SignProto, args.ClickLocation.SnapToGrid(EntityManager));
        var xform = Transform(holoUid);
        if (!xform.Anchored)
            _transform.AnchorEntity(holoUid, xform); // anchor to prevent any tempering with (don't know what could even interact with it)

        // WD EDIT START
        EnsureComp<HolosignComponent>(holoUid, out var holosign);
        --component.Uses;
        component.Signs.Add(holoUid);
        holosign.Projector = uid;
        // WD EDIT END

        args.Handled = true;
    }

    // WD EDIT START
    private void OnUse(EntityUid uid, HolosignProjectorComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        foreach (var sign in component.Signs.ToList())
        {
            component.Signs.Remove(sign);
            QueueDel(sign);
        }

        args.Handled = true;
        component.Uses = component.MaxUses;
        _popup.PopupEntity(Loc.GetString("holoprojector-delete-signs"), args.User, args.User, PopupType.Medium);
    }
    // WD EDIT START
}
