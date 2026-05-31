using System.Linq;
using Content.Shared._White.Appearance;
using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Appearance.Systems;
using Content.Shared._White.Body;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Markings.Systems;
using Content.Shared._White.MagicMirror.Components;
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

namespace Content.Shared._White.MagicMirror.Systems;

public sealed class MagicMirrorSystem : EntitySystem
{
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

        SubscribeLocalEvent<MagicMirrorComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        SubscribeLocalEvent<MagicMirrorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MagicMirrorComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<MagicMirrorComponent, BoundUserInterfaceCheckRangeEvent>(OnBoundUserInterfaceCheckRange);
        SubscribeLocalEvent<MagicMirrorComponent, MagicMirrorSelectDoAfterEvent>(OnSelectDoAfter);

        Subs.BuiEvents<MagicMirrorComponent>(
            MagicMirrorUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
                subs.Event<MagicMirrorSelectMessage>(OnSelect);
            });
    }

    #region Event Handling

    private void OnActivatableUIOpenAttempt(Entity<MagicMirrorComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (HasComp<BodyAppearanceComponent>(ent.Comp.Target ?? args.User))
            return;

        args.Cancel();
    }

    private void OnAfterInteract(Entity<MagicMirrorComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        UpdateInterface(ent, args.Target.Value);
        _userInterface.TryOpenUi(ent.Owner, MagicMirrorUiKey.Key, args.User);
    }

    private void OnBeforeActivatableUIOpen(Entity<MagicMirrorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateInterface(ent, args.User);
    }

    private void OnBoundUserInterfaceCheckRange(Entity<MagicMirrorComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
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

    private void OnSelectDoAfter(Entity<MagicMirrorComponent> ent, ref MagicMirrorSelectDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (ent.Comp.Target != args.Target)
            return;

        foreach (var (category, markings) in args.Markings)
        {
            if (!ent.Comp.Categories.Contains(category))
            {
                args.Markings.Remove(category);
                continue;
            }

            foreach (var marking in markings.ToList())
            {
                if (ent.Comp.Layers.Contains(marking.Layer))
                    continue;

                markings.Remove(marking);
            }
        }

        _markings.ApplyMarkings(args.Target.Value, args.Markings);
    }

    private void OnBoundUIClosed(Entity<MagicMirrorComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
        DirtyField(ent, ent.Comp, nameof(MagicMirrorComponent.Target));
    }

    private void OnSelect(Entity<MagicMirrorComponent> ent, ref MagicMirrorSelectMessage args)
    {
        if (ent.Comp.Target is not { } target)
            return;

        if (CheckHeadSlotOrClothes(target))
        {
            _popup.PopupEntity(
                ent.Comp.Target == args.Actor
                    ? Loc.GetString("magic-mirror-blocked-by-hat-self")
                    : Loc.GetString("magic-mirror-blocked-by-hat-self-target", ("target", Identity.Entity(args.Actor, EntityManager))),
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
        if (ent.Comp.Target == args.Actor)
            doafterTime /= ent.Comp.SelfTimeMultiply;

        var doAfter = new MagicMirrorSelectDoAfterEvent
        {
            Markings = args.MarkingSet,
        };

        _doAfter.TryStartDoAfter(
            new DoAfterArgs(EntityManager, args.Actor, doafterTime, doAfter, ent, target: target, used: ent)
            {
                DistanceThreshold = SharedInteractionSystem.InteractionRange,
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
            },
            out var doAfterId);

        _popup.PopupEntity(
            target == args.Actor
                ? Loc.GetString("magic-mirror-change-slot-self")
                : Loc.GetString(
                    "magic-mirror-change-slot-target",
                    ("user", Identity.Entity(args.Actor, EntityManager))),
            target,
            target,
            PopupType.Medium);

        ent.Comp.DoAfter = doAfterId?.Index;
        _audio.PlayPredicted(ent.Comp.Sound, ent, args.Actor);
    }

    #endregion

    #region Public API

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

    private void UpdateInterface(Entity<MagicMirrorComponent> ent, EntityUid target)
    {
        if (!_bodyAppearance.TryGetData(target, out var appearanceData))
            return;

        if (!_markings.TryGetData(target, ent.Comp.Layers, out var markingsSets, out var markingsData))
            return;

        ent.Comp.Target = target;

        foreach (var markingSet in markingsSets)
        {
            if (ent.Comp.Categories.Contains(markingSet.Key))
                continue;

            markingsSets.Remove(markingSet.Key);
        }

        foreach (var markingData in markingsData)
        {
            if (ent.Comp.Categories.Contains(markingData.Key))
                continue;

            markingsData.Remove(markingData.Key);
        }

        var state = new MagicMirrorUiState(appearanceData, markingsSets, markingsData);
        _userInterface.SetUiState(ent.Owner, MagicMirrorUiKey.Key, state);

        Dirty(ent);
    }

    #endregion
}

[Serializable, NetSerializable]
public enum MagicMirrorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MagicMirrorSelectMessage(
    Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markingSet)
    : BoundUserInterfaceMessage
{
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> MarkingSet { get; } = markingSet;
}


[Serializable, NetSerializable]
public sealed class MagicMirrorUiState(
    Dictionary<BodyProviderType, BodyAppearanceData> appearanceData,
    Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markings,
    Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> markingsData)
    : BoundUserInterfaceState
{
    public NetEntity Target;

    public Dictionary<BodyProviderType, BodyAppearanceData> AppearanceData { get; } = appearanceData;
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings { get; } = markings;
    public Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> MarkingsData { get; } = markingsData;
}

[Serializable, NetSerializable]
public sealed partial class MagicMirrorSelectDoAfterEvent : DoAfterEvent
{
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings;

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
