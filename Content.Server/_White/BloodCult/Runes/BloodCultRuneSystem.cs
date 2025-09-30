using System.Linq;
using System.Numerics;
using Content.Server._White.GameTicking.Rules;
using Content.Server.Bible.Components;
using Content.Server.Chat.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Popups;
using Content.Shared._Goobstation.Bible;
using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared._White.BloodCult.Empower;
using Content.Shared._White.BloodCult.Runes;
using Content.Shared._White.BloodCult.Runes.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Station.Components;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._White.BloodCult.Runes;

public sealed class BloodCultRuneSystem : SharedBloodCultRuneSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, DrawRuneDoAfter>(OnDrawRune);

        SubscribeLocalEvent<RuneDrawerComponent, BeforeActivatableUIOpenEvent>(BeforeOpenUi);
        SubscribeLocalEvent<RuneDrawerComponent, RuneDrawerSelectedMessage>(OnRuneSelected);

        SubscribeLocalEvent<BloodCultRuneComponent, ActivateInWorldEvent>(OnRuneActivate);
        SubscribeLocalEvent<BloodCultRuneComponent, AfterActivateInWorldEvent>(AfterRuneActivate);
        SubscribeLocalEvent<BloodCultRuneComponent, InteractUsingEvent>(OnRuneInteractUsing);
        SubscribeLocalEvent<BloodCultRuneComponent, RuneEraseDoAfterEvent>(OnRuneErase);
        SubscribeLocalEvent<BloodCultRuneComponent, StartCollideEvent>(OnRuneCollide);
    }

    private void OnDrawRune(Entity<BloodCultistComponent> ent, ref DrawRuneDoAfter args)
    {
        if (args.Cancelled || !_protoManager.TryIndex(args.Rune, out var runeSelector))
            return;

        DealDamage(args.User, runeSelector.DrawDamage);

        _audio.PlayPvs(args.EndDrawingSound, args.User, AudioParams.Default.WithMaxDistance(2f));
        var runeEnt = SpawnRune(args.User, runeSelector.Prototype);
        if (TryComp(runeEnt, out BloodCultRuneComponent? rune)
            && rune.TriggerRendingMarkers
            && !_bloodCultRule.TryConsumeNearestMarker(ent))
            return;

        var ev = new AfterRunePlaced(args.User);
        RaiseLocalEvent(runeEnt, ev);
    }

    private void BeforeOpenUi(Entity<RuneDrawerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        var availableRunes = new List<ProtoId<BloodCultRunePrototype>>();
        var runeSelectorArray = _protoManager.EnumeratePrototypes<BloodCultRunePrototype>().OrderBy(r => r.ID).ToArray();

        foreach (var runeSelector in runeSelectorArray)
        {
            if (runeSelector.RequireTargetDead && !_bloodCultRule.IsObjectiveFinished() ||
                runeSelector.RequiredTotalCultists > _bloodCultRule.GetTotalCultists())
                continue;

            availableRunes.Add(runeSelector.ID);
        }

        _userInterface.SetUiState(ent.Owner, RuneDrawerBuiKey.Key, new RuneDrawerMenuState(availableRunes));
    }

    private void OnRuneSelected(Entity<RuneDrawerComponent> ent, ref RuneDrawerSelectedMessage args)
    {
        if (!_protoManager.TryIndex(args.SelectedRune, out var runeSelector) || EntityWhitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Actor))
            return;

        if (!HasComp<StationMemberComponent>(Transform(ent).GridUid))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-rune-cant-draw"), args.Actor, args.Actor);
            return;
        }

        if (runeSelector.RequireTargetDead && !_bloodCultRule.CanDrawRendingRune(args.Actor))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-rune-cant-draw-rending"), args.Actor, args.Actor);
            return;
        }

        var timeToDraw = runeSelector.DrawTime;
        if (TryComp(args.Actor, out BloodCultEmpoweredComponent? empowered))
            timeToDraw *= empowered.RuneTimeMultiplier;

        var ev = new DrawRuneDoAfter
        {
            Rune = args.SelectedRune,
            EndDrawingSound = ent.Comp.EndDrawingSound
        };

        var argsDoAfterEvent = new DoAfterArgs(EntityManager, args.Actor, timeToDraw, ev, args.Actor)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(argsDoAfterEvent))
            _audio.PlayPredicted(ent.Comp.StartDrawingSound, ent, args.Actor, AudioParams.Default.WithMaxDistance(2f));
    }

    private void OnRuneActivate(Entity<BloodCultRuneComponent> rune, ref ActivateInWorldEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.User))
            return;

        var cultists = GatherCultists(rune, rune.Comp.RuneActivationRange);
        if (cultists.Count < rune.Comp.RequiredInvokers)
            return;

        var invokeRuneEvent = new InvokeRuneEvent(args.User, cultists);
        RaiseLocalEvent(rune, invokeRuneEvent);
        if (invokeRuneEvent.Cancelled)
            return;

        foreach (var cultist in cultists)
        {
            DealDamage(cultist, rune.Comp.ActivationDamage);

            if (!string.IsNullOrEmpty(rune.Comp.InvokePhrase))
            {
                _chat.TrySendInGameICMessage(
                    cultist,
                    rune.Comp.InvokePhrase,
                    rune.Comp.InvokeChatType,
                    false,
                    checkRadioPrefix: false);
            }
        }

        args.Handled = true;
    }

    private void AfterRuneActivate(Entity<BloodCultRuneComponent> rune, ref AfterActivateInWorldEvent args)
    {
        var afterInvokeRuneEvent = new AfterInvokeRuneEvent(args.User);
        RaiseLocalEvent(rune, afterInvokeRuneEvent);
    }

    private void OnRuneInteractUsing(Entity<BloodCultRuneComponent> rune, ref InteractUsingEvent args)
    {
        if (!rune.Comp.CanBeErased)
            return;

        // Logic for bible erasing
        if (TryComp<BibleComponent>(args.Used, out var bible) && HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-rune-erased"), rune, args.User);
            _audio.PlayPvs(bible.HealSoundPath, args.User);
            EntityManager.DeleteEntity(args.Target);
            return;
        }

        if (!TryComp(args.Used, out RuneDrawerComponent? runeDrawer))
            return;

        var argsDoAfterEvent =
            new DoAfterArgs(EntityManager, args.User, runeDrawer.EraseTime, new RuneEraseDoAfterEvent(), rune)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        if (_doAfter.TryStartDoAfter(argsDoAfterEvent))
            _popup.PopupEntity(Loc.GetString("blood-cult-rune-started-erasing"), rune, args.User);
    }

    private void OnRuneErase(Entity<BloodCultRuneComponent> ent, ref RuneEraseDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _popup.PopupEntity(Loc.GetString("blood-cult-rune-erased"), ent, args.User);
        QueueDel(ent);
    }

    private void OnRuneCollide(Entity<BloodCultRuneComponent> rune, ref StartCollideEvent args)
    {
        if (!rune.Comp.CanBeErased ||
            !TryComp<SolutionContainerManagerComponent>(args.OtherEntity, out var solutionContainer) ||
            !HasComp<VaporComponent>(args.OtherEntity) && !HasComp<SprayComponent>(args.OtherEntity))
            return;

        if (_solutionContainer.EnumerateSolutions((args.OtherEntity, solutionContainer))
            .Any(solution => solution.Solution.Comp.Solution.ContainsPrototype(rune.Comp.HolyWaterPrototype)))
            QueueDel(rune);
    }

    private void DealDamage(EntityUid user, DamageSpecifier? damage = null)
    {
        if (damage is null)
            return;

        // So the original DamageSpecifier will not be changed.
        var newDamage = new DamageSpecifier(damage);
        if (TryComp(user, out BloodCultEmpoweredComponent? empowered))
        {
            foreach (var (key, value) in damage.DamageDict)
                damage.DamageDict[key] = value * empowered.RuneDamageMultiplier;
        }

        _damageable.TryChangeDamage(user, newDamage, true);
    }

    private EntityUid SpawnRune(EntityUid user, EntProtoId rune)
    {
        var transform = Transform(user);
        var snappedLocalPosition = new Vector2(
            MathF.Floor(transform.LocalPosition.X) + 0.5f,
            MathF.Floor(transform.LocalPosition.Y) + 0.5f);
        var spawnPosition = _transform.GetMapCoordinates(user);
        var runeEntity = EntityManager.Spawn(rune, spawnPosition);
        _transform.SetLocalPosition(runeEntity, snappedLocalPosition);

        return runeEntity;
    }
}
