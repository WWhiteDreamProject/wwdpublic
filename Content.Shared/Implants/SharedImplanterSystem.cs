using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Implants;

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, ComponentInit>(OnImplanterInit);
        SubscribeLocalEvent<ImplanterComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ImplanterComponent, ExaminedEvent>(OnExamine);
    }

    private void OnImplanterInit(EntityUid uid, ImplanterComponent component, ComponentInit args)
    {
        if (component.Implant != null)
            component.ImplanterSlot.StartingItem = component.Implant;

        _itemSlots.AddItemSlot(uid, ImplanterComponent.ImplanterSlotId, component.ImplanterSlot);
    }

    private void OnEntInserted(EntityUid uid, ImplanterComponent component, EntInsertedIntoContainerMessage args)
    {
        var implantData = EntityManager.GetComponent<MetaDataComponent>(args.Entity);
        component.ImplantData = (implantData.EntityName, implantData.EntityDescription);
    }

    private void OnExamine(EntityUid uid, ImplanterComponent component, ExaminedEvent args)
    {
        if (!component.ImplanterSlot.HasItem || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("implanter-contained-implant-text", ("desc", component.ImplantData.Item2)));
    }

    //Instantly implant something and add all necessary components and containers.
    //Set to draw mode if not implant only
    public void Implant(EntityUid user, EntityUid target, EntityUid implanter, ImplanterComponent component)
    {
        if (!CanImplant(user, target, implanter, component, out var implant, out var implantComp, out _))
            return;

        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        if (component.ImplanterSlot.ContainerSlot != null)
            _container.Remove(implant.Value, component.ImplanterSlot.ContainerSlot);
        implantComp.ImplantedEntity = target;
        implantContainer.OccludesLight = false;
        _container.Insert(implant.Value, implantContainer);

        RaiseLocalEvent(implant.Value, new SubdermalImplantInserted(user, target)); // WD EDIT

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            DrawMode(implanter, component);
        else
            ImplantMode(implanter, component);

        var ev = new TransferDnaEvent { Donor = target, Recipient = implanter };
        RaiseLocalEvent(target, ref ev);

        Dirty(implanter, component);
    }

    public bool CanImplant(
        EntityUid user,
        EntityUid target,
        EntityUid implanter,
        ImplanterComponent component,
        [NotNullWhen(true)] out EntityUid? implant,
        [NotNullWhen(true)] out SubdermalImplantComponent? implantComp,
        out bool popup) // WD EDIT
    {
        implant = component.ImplanterSlot.ContainerSlot?.ContainedEntities.FirstOrNull();
        popup = true; // WD EDIF
        if (!TryComp(implant, out implantComp))
            return false;

        if (!CheckTarget(target, component.Whitelist, component.Blacklist) ||
            !CheckTarget(target, implantComp.Whitelist, implantComp.Blacklist))
        {
            return false;
        }

        // WD EDIT START
        var ev = new AttemptSubdermalImplantInserted(user, target);
        RaiseLocalEvent(implant.Value, ev);

        popup = ev.Popup;
        // WD EDIT END
        return !ev.Cancelled;
    }

    protected bool CheckTarget(EntityUid target, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        return _whitelistSystem.IsWhitelistPassOrNull(whitelist, target) &&
            _whitelistSystem.IsBlacklistFailOrNull(blacklist, target);
    }

    //Draw the implant out of the target
    //TODO: Rework when surgery is in so implant cases can be a thing
    public void Draw(EntityUid implanter, EntityUid user, EntityUid target, ImplanterComponent component)
    {
        var implanterContainer = component.ImplanterSlot.ContainerSlot;

        if (implanterContainer is null)
            return;

        var permanentFound = false;

        if (_container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            var implantCompQuery = GetEntityQuery<SubdermalImplantComponent>();

            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (!implantCompQuery.TryGetComponent(implant, out var implantComp))
                    continue;

                //Don't remove a permanent implant and look for the next that can be drawn
                if (!_container.CanRemove(implant, implantContainer))
                {
                    var implantName = Identity.Entity(implant, EntityManager);
                    var targetName = Identity.Entity(target, EntityManager);
                    var failedPermanentMessage = Loc.GetString("implanter-draw-failed-permanent",
                        ("implant", implantName), ("target", targetName));
                    _popup.PopupEntity(failedPermanentMessage, target, user);
                    permanentFound = implantComp.Permanent;
                    continue;
                }

                _container.Remove(implant, implantContainer);
                implantComp.ImplantedEntity = null;
                _container.Insert(implant, implanterContainer);
                permanentFound = implantComp.Permanent;

                RaiseLocalEvent(implant, new SubdermalImplantRemoved(user, target)); // WD EDIT

                var ev = new TransferDnaEvent { Donor = target, Recipient = implanter };
                RaiseLocalEvent(target, ref ev);

                //Break so only one implant is drawn
                break;
            }

            if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly && !permanentFound)
                ImplantMode(implanter, component);

            Dirty(implanter, component);
        }
    }

    private void ImplantMode(EntityUid uid, ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Inject;
        ChangeOnImplantVisualizer(uid, component);
    }

    private void DrawMode(EntityUid uid, ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Draw;
        ChangeOnImplantVisualizer(uid, component);
    }

    private void ChangeOnImplantVisualizer(EntityUid uid, ImplanterComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        bool implantFound;

        if (component.ImplanterSlot.HasItem)
            implantFound = true;

        else
            implantFound = false;

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);

        else if (component.CurrentMode == ImplanterToggleMode.Inject && component.ImplantOnly)
        {
            _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);
            _appearance.SetData(uid, ImplanterImplantOnlyVisuals.ImplantOnly, component.ImplantOnly,
                appearance);
        }

        else
            _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);
    }
}

[Serializable, NetSerializable]
public sealed partial class ImplantEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class DrawEvent : SimpleDoAfterEvent
{
}

// WD EDIT START
public sealed class AttemptSubdermalImplantInserted(EntityUid user, EntityUid target, bool popup = true) : CancellableEntityEventArgs
{
    /// <summary>
    ///     Entity who implants
    /// </summary>
    public EntityUid User = user;

    /// <summary>
    ///     Entity being implanted
    /// </summary>
    public EntityUid Target = target;

    /// <summary>
    ///     Popup if implanted failed
    /// </summary>
    public bool Popup = popup;
}

public sealed class SubdermalImplantInserted(EntityUid user, EntityUid target)
{
    /// <summary>
    ///     Entity who implants
    /// </summary>
    public EntityUid User = user;

    /// <summary>
    ///     Entity being implanted
    /// </summary>
    public EntityUid Target = target;
}

public sealed class SubdermalImplantRemoved(EntityUid user, EntityUid target)
{
    /// <summary>
    ///     Entity who removes implant
    /// </summary>
    public EntityUid User = user;

    /// <summary>
    ///     Entity which implant is removing
    /// </summary>
    public EntityUid Target = target;
}
// WD EDIT END
