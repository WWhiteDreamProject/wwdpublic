using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Appearance.Systems;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Markings.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._White.Humanoid.Markings.Systems;

public sealed class MarkingsModifierSystem : EntitySystem
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodyAppearanceSystem _bodyAppearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMarkingsSystem _markings = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> HidesHairTag = "HidesHair";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarkingsModifierComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        SubscribeLocalEvent<MarkingsModifierComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MarkingsModifierComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<MarkingsModifierComponent, BoundUserInterfaceCheckRangeEvent>(OnBoundUserInterfaceCheckRange);
        SubscribeLocalEvent<MarkingsModifierComponent, MarkingModifierMarkingSetDoAfterEvent>(OnSelectDoAfter);

        Subs.BuiEvents<MarkingsModifierComponent>(
            MarkingModifierKey.Key,
            subs =>
            {
                subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
                subs.Event<MarkingModifierMarkingSetMessage>(OnSelect);
            });
    }

    #region Event Handling

    private void OnActivatableUIOpenAttempt(Entity<MarkingsModifierComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (HasComp<BodyAppearanceComponent>(ent.Comp.Target ?? args.User))
            return;

        args.Cancel();
    }

    private void OnAfterInteract(Entity<MarkingsModifierComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        UpdateInterface(ent, args.Target.Value);
        _userInterface.TryOpenUi(ent.Owner, MarkingModifierKey.Key, args.User);
    }

    private void OnBeforeActivatableUIOpen(Entity<MarkingsModifierComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateInterface(ent, args.User);
    }

    private void OnBoundUserInterfaceCheckRange(Entity<MarkingsModifierComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.Result == BoundUserInterfaceRangeResult.Fail)
            return;

        if (!Exists(ent.Comp.Target))
        {
            ent.Comp.Target = null;
            args.Result = BoundUserInterfaceRangeResult.Fail;
            return;
        }

        if (_interaction.InRangeUnobstructed(ent.Comp.Target.Value, ent.Owner))
            return;

        args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void OnSelectDoAfter(Entity<MarkingsModifierComponent> ent, ref MarkingModifierMarkingSetDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (ent.Comp.Target != args.Target)
            return;

        var markings = new List<Marking>();
        foreach (var (category, categoryMarkings) in args.Markings)
        {
            if (!ent.Comp.Categories.Contains(category))
                continue;

            markings.AddRange(categoryMarkings);
        }

        _markings.ApplyMarkings(args.Target.Value, markings);

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.User);
    }

    private void OnBoundUIClosed(Entity<MarkingsModifierComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
        DirtyField(ent, ent.Comp, nameof(MarkingsModifierComponent.Target));
    }

    private void OnSelect(Entity<MarkingsModifierComponent> ent, ref MarkingModifierMarkingSetMessage args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (ent.Comp.Target is not { } target)
            return;

        if (CheckHeadSlotOrClothes(target))
        {
            _popup.PopupClient(
                Loc.GetString("magic-mirror-blocked-by-hat"),
                args.Actor,
                args.Actor,
                PopupType.Medium);
            return;
        }

        if (ent.Comp.DoAfter.HasValue)
        {
            _doAfter.Cancel(target, ent.Comp.DoAfter.Value);
            ent.Comp.DoAfter = null;
        }

        var doafterTime = ent.Comp.Time;
        var self = ent.Comp.Target == args.Actor;

        if (self)
            doafterTime *= ent.Comp.SelfTimeMultiply;

        var doAfter = new MarkingModifierMarkingSetDoAfterEvent
        {
            Markings = args.Markings,
        };

        _doAfter.TryStartDoAfter(
            new DoAfterArgs(EntityManager, args.Actor, doafterTime, doAfter, ent, target, ent)
            {
                DistanceThreshold = SharedInteractionSystem.InteractionRange,
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
            },
            out var doAfterId);

        if (self)
        {
            _popup.PopupClient(
                Loc.GetString("magic-mirror-change-slot-self"),
                target,
                target,
                PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString(
                    "magic-mirror-change-slot-target",
                    ("user", Identity.Entity(args.Actor, EntityManager))),
                target,
                target,
                PopupType.Medium);
        }

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.Actor);

        ent.Comp.DoAfter = doAfterId?.Index;
    }

    #endregion

    #region Public API

    // TODO:
    private bool CheckHeadSlotOrClothes(EntityUid target)
    {
        if (!TryComp<InventoryComponent>(target, out var inventoryComp))
            return false;

        // any hat whatsoever will block haircutting
        if (_inventory.TryGetSlotEntity(target, "head", out _, inventoryComp))
        {
            return true;
        }

        // maybe there's some kind of armor that has the HidesHair tag as well, so check every slot for it
        var slots = _inventory.GetSlotEnumerator((target, inventoryComp), SlotFlags.WITHOUT_POCKET);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity != null && _tag.HasTag(slot.ContainedEntity.Value, HidesHairTag))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateInterface(Entity<MarkingsModifierComponent> ent, EntityUid target)
    {
        if (!_bodyAppearance.TryGetAppearance(target, out var appearanceData))
            return;

        if (!_markings.TryGetData(target, out var markingsSet, out var markingsData))
            return;

        var layers = _marking.GetLayers(ent.Comp.Categories);
        foreach (var layer in appearanceData.Keys)
        {
            if (layers.Contains(layer))
                continue;

            appearanceData.Remove(layer);
        }

        foreach (var layer in markingsData.Keys)
        {
            if (layers.Contains(layer))
                continue;

            markingsData.Remove(layer);
        }

        ent.Comp.Target = target;

        var markings = _marking.GetMarkingsByCategory(markingsSet);
        var state = new MarkingModifierState(appearanceData, markingsData, markings);
        _userInterface.SetUiState(ent.Owner, MarkingModifierKey.Key, state);

        Dirty(ent);
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class MarkingModifierMarkingSetDoAfterEvent : DoAfterEvent
{
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings;

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
