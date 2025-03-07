using System.Numerics;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Timing;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Fluids.EntitySystems;

/// <inheritdoc/>
public sealed partial class AbsorbentSystem : SharedAbsorbentSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!; // WD EDIT

    public const float AbsorptionRange = 0.25f; // WD EDIT

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, ComponentInit>(OnAbsorbentInit);
        SubscribeLocalEvent<AbsorbentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<AbsorbentComponent, UserActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<AbsorbentComponent, SolutionContainerChangedEvent>(OnAbsorbentSolutionChange);
    }

    private void OnAbsorbentInit(EntityUid uid, AbsorbentComponent component, ComponentInit args)
    {
        // TODO: I know dirty on init but no prediction moment.
        UpdateAbsorbent(uid, component);
    }

    private void OnAbsorbentSolutionChange(EntityUid uid, AbsorbentComponent component, ref SolutionContainerChangedEvent args)
    {
        UpdateAbsorbent(uid, component);
    }

    private void UpdateAbsorbent(EntityUid uid, AbsorbentComponent component)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, AbsorbentComponent.SolutionName, out _, out var solution))
            return;

        var oldProgress = component.Progress.ShallowClone();
        component.Progress.Clear();

        var water = solution.GetTotalPrototypeQuantity(PuddleSystem.EvaporationReagents);
        if (water > FixedPoint2.Zero)
        {
            component.Progress[solution.GetColorWithOnly(_prototype, PuddleSystem.EvaporationReagents)] = water.Float();
        }

        var otherColor = solution.GetColorWithout(_prototype, PuddleSystem.EvaporationReagents);
        var other = (solution.Volume - water).Float();

        if (other > 0f)
        {
            component.Progress[otherColor] = other;
        }

        var remainder = solution.AvailableVolume;

        if (remainder > FixedPoint2.Zero)
        {
            component.Progress[Color.DarkGray] = remainder.Float();
        }

        if (component.Progress.Equals(oldProgress))
            return;

        Dirty(uid, component);
    }

    private void OnActivateInWorld(EntityUid uid, AbsorbentComponent component, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        Mop(uid, uid, component, args.Target.ToCoordinates(), args.Target); // WD EDIT
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, AbsorbentComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        Mop(args.User, args.Used, component, args.ClickLocation, args.Target);
        args.Handled = true;
    }

    public void Mop(EntityUid user, EntityUid used, AbsorbentComponent component, EntityCoordinates coordinates, EntityUid? target = null)
    {
        if (!_solutionContainerSystem.TryGetSolution(used, AbsorbentComponent.SolutionName, out var absorberSoln))
            return;

        if (TryComp<UseDelayComponent>(used, out var useDelay)
            && _useDelay.IsDelayed((used, useDelay)))
            return;

        // If it's a puddle try to grab from
        if (TryPuddleInteract(user, used, component, useDelay, absorberSoln.Value, coordinates))
            return;

        // If it's refillable try to transfer
        if (!target.HasValue)
            return;

        TryRefillableInteract(user, used, target.Value, component, useDelay, absorberSoln.Value);
    }

    /// <summary>
    ///     Logic for an absorbing entity interacting with a refillable.
    /// </summary>
    public bool TryRefillableInteract(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent component, UseDelayComponent? useDelay, Entity<SolutionComponent> absorbentSoln)
    {
        if (!TryComp(target, out RefillableSolutionComponent? refillable))
            return false;

        if (!_solutionContainerSystem.TryGetRefillableSolution((target, refillable, null), out var refillableSoln, out var refillableSolution))
            return false;

        if (refillableSolution.Volume <= 0)
        {
            // Target empty - only transfer absorbent contents into refillable
            if (!TryTransferFromAbsorbentToRefillable(user, used, target, component, absorbentSoln, refillableSoln.Value))
                return false;
        }
        else
        {
            // Target non-empty - do a two-way transfer
            if (!TryTwoWayAbsorbentRefillableTransfer(user, used, target, component, absorbentSoln, refillableSoln.Value))
                return false;
        }

        _audio.PlayPvs(component.TransferSound, target);
        if (useDelay != null)
            _useDelay.TryResetDelay((used, useDelay));
        return true;
    }

    /// <summary>
    ///     Logic for an transferring solution from absorber to an empty refillable.
    /// </summary>
    private bool TryTransferFromAbsorbentToRefillable(
        EntityUid user,
        EntityUid used,
        EntityUid target,
        AbsorbentComponent component,
        Entity<SolutionComponent> absorbentSoln,
        Entity<SolutionComponent> refillableSoln)
    {
        var absorbentSolution = absorbentSoln.Comp.Solution;
        if (absorbentSolution.Volume <= 0)
        {
            _popups.PopupEntity(Loc.GetString("mopping-system-target-container-empty", ("target", target)), user, user);
            return false;
        }

        var refillableSolution = refillableSoln.Comp.Solution;
        var transferAmount = component.PickupAmount < refillableSolution.AvailableVolume ?
            component.PickupAmount :
            refillableSolution.AvailableVolume;

        if (transferAmount <= 0)
        {
            _popups.PopupEntity(Loc.GetString("mopping-system-full", ("used", used)), used, user);
            return false;
        }

        // Prioritize transferring non-evaporatives if absorbent has any
        var contaminants = _solutionContainerSystem.SplitSolutionWithout(absorbentSoln, transferAmount, PuddleSystem.EvaporationReagents);
        if (contaminants.Volume > 0)
        {
            _solutionContainerSystem.TryAddSolution(refillableSoln, contaminants);
        }
        else
        {
            var evaporatives = _solutionContainerSystem.SplitSolution(absorbentSoln, transferAmount);
            _solutionContainerSystem.TryAddSolution(refillableSoln, evaporatives);
        }

        return true;
    }

    /// <summary>
    ///     Logic for an transferring contaminants to a non-empty refillable & reabsorbing water if any available.
    /// </summary>
    private bool TryTwoWayAbsorbentRefillableTransfer(
        EntityUid user,
        EntityUid used,
        EntityUid target,
        AbsorbentComponent component,
        Entity<SolutionComponent> absorbentSoln,
        Entity<SolutionComponent> refillableSoln)
    {
        var contaminantsFromAbsorbent = _solutionContainerSystem.SplitSolutionWithout(absorbentSoln, component.PickupAmount, PuddleSystem.EvaporationReagents);

        var absorbentSolution = absorbentSoln.Comp.Solution;
        if (contaminantsFromAbsorbent.Volume == FixedPoint2.Zero && absorbentSolution.AvailableVolume == FixedPoint2.Zero)
        {
            // Nothing to transfer to refillable and no room to absorb anything extra
            _popups.PopupEntity(Loc.GetString("mopping-system-puddle-space", ("used", used)), user, user);

            // We can return cleanly because nothing was split from absorbent solution
            return false;
        }

        var waterPulled = component.PickupAmount < absorbentSolution.AvailableVolume ?
            component.PickupAmount :
            absorbentSolution.AvailableVolume;

        var refillableSolution = refillableSoln.Comp.Solution;
        var waterFromRefillable = refillableSolution.SplitSolutionWithOnly(waterPulled, PuddleSystem.EvaporationReagents);
        _solutionContainerSystem.UpdateChemicals(refillableSoln);

        if (waterFromRefillable.Volume == FixedPoint2.Zero && contaminantsFromAbsorbent.Volume == FixedPoint2.Zero)
        {
            // Nothing to transfer in either direction
            _popups.PopupEntity(Loc.GetString("mopping-system-target-container-empty-water", ("target", target)), user, user);

            // We can return cleanly because nothing was split from refillable solution
            return false;
        }

        var anyTransferOccurred = false;

        if (waterFromRefillable.Volume > FixedPoint2.Zero)
        {
            // transfer water to absorbent
            _solutionContainerSystem.TryAddSolution(absorbentSoln, waterFromRefillable);
            anyTransferOccurred = true;
        }

        if (contaminantsFromAbsorbent.Volume > 0)
        {
            if (refillableSolution.AvailableVolume <= 0)
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-full", ("used", target)), user, user);
            }
            else
            {
                // transfer as much contaminants to refillable as will fit
                var contaminantsForRefillable = contaminantsFromAbsorbent.SplitSolution(refillableSolution.AvailableVolume);
                _solutionContainerSystem.TryAddSolution(refillableSoln, contaminantsForRefillable);
                anyTransferOccurred = true;
            }

            // absorb everything that did not fit in the refillable back by the absorbent
            _solutionContainerSystem.TryAddSolution(absorbentSoln, contaminantsFromAbsorbent);
        }

        return anyTransferOccurred;
    }

    /// <summary>
    ///     Logic for an absorbing entity interacting with a puddle.
    /// </summary>
    private bool TryPuddleInteract(EntityUid user, EntityUid used, AbsorbentComponent absorber, UseDelayComponent? useDelay, Entity<SolutionComponent> absorberSoln, EntityCoordinates coordinates)
    {
        // WD EDIT START
        if (!_mapManager.TryFindGridAt(coordinates.ToMap(EntityManager, _transform), out var gridUid, out var mapGrid))
            return false;

        var tileRef = _mapSystem.GetTileRef(gridUid, mapGrid, coordinates);
        var tile = new EntityCoordinates(coordinates.EntityId, new (tileRef.X + 0.5f, tileRef.Y + 0.5f));
        var targets = new HashSet<Entity<PuddleComponent>>();
        _lookup.GetEntitiesInRange(tile, AbsorptionRange, targets, LookupFlags.Dynamic | LookupFlags.Uncontained);

        if (targets.Count == 0)
            return false;

        var playSound = false;
        // WD EDIT END

        foreach (var (entity, component) in targets) // WD EDIT
        {
            if (!_solutionContainerSystem.ResolveSolution(entity, component.SolutionName, ref component.Solution, out var puddleSolution) || puddleSolution.Volume <= 0)
                continue;

            // Check if the puddle has any non-evaporative reagents
            if (_puddleSystem.CanFullyEvaporate(puddleSolution))
                continue;

            // Check if we have any evaporative reagents on our absorber to transfer
            var absorberSolution = absorberSoln.Comp.Solution;
            var available = absorberSolution.GetTotalPrototypeQuantity(PuddleSystem.EvaporationReagents);

            // No material
            if (available == FixedPoint2.Zero)
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-no-water", ("used", used)), user, user);
                break;
            }

            var transferMax = absorber.PickupAmount;
            var transferAmount = available > transferMax ? transferMax : available;

            var puddleSplit = puddleSolution.SplitSolutionWithout(transferAmount, PuddleSystem.EvaporationReagents);
            var absorberSplit = absorberSolution.SplitSolutionWithOnly(puddleSplit.Volume, PuddleSystem.EvaporationReagents);

            // Do tile reactions first
            _puddleSystem.DoTileReactions(tileRef, absorberSplit);

            _solutionContainerSystem.AddSolution(component.Solution.Value, absorberSplit);
            _solutionContainerSystem.AddSolution(absorberSoln, puddleSplit);

            playSound = true; // WD EDIT
        }

        // WD EDIT START
        if (!playSound)
            return false;
        // WD EDIT END
        _audio.PlayPvs(absorber.PickupSound, used);

        if (useDelay != null)
            _useDelay.TryResetDelay((used, useDelay));

        var userXform = Transform(user);
        var targetPos = tile.Position; // WD EDIT
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
        localPos = userXform.LocalRotation.RotateVec(localPos);

        _melee.DoLunge(user, used, Angle.Zero, localPos, null, Angle.Zero, false);

        return true;
    }
}
