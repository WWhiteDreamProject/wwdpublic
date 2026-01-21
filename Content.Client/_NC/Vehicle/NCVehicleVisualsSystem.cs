using System.Diagnostics.CodeAnalysis;
using Content.Shared._NC.Vehicle;
using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Vehicle;

public sealed class NCVehicleVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StrapComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<StrapComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<StrapComponent, EntParentChangedMessage>(OnStrapParentChanged);
    }

    private readonly HashSet<EntityUid> _hiddenEntities = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Track entities that SHOULD be hidden this frame
        var currentHidden = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<NCVehicleComponent>();
        while (query.MoveNext(out var uid, out var ncVehicle))
        {
            if (!ncVehicle.HidePassengers)
                continue;

            // 1. Check Driver (Strap on the vehicle itself)
            if (TryComp<StrapComponent>(uid, out var carStrap))
            {
                foreach (var buckled in carStrap.BuckledEntities)
                {
                    // Double check they are actually buckled to us
                    if (TryComp<BuckleComponent>(buckled, out var buckle) && buckle.BuckledTo != uid)
                        continue;

                    currentHidden.Add(buckled);
                }
            }

            // 2. Check Passengers (Strap on seats)
            foreach (var seatUid in ncVehicle.SpawnedSeats)
            {
                if (TryComp<StrapComponent>(seatUid, out var seatStrap))
                {
                    foreach (var buckled in seatStrap.BuckledEntities)
                    {
                        if (TryComp<BuckleComponent>(buckled, out var buckle) && buckle.BuckledTo != seatUid)
                            continue;

                        currentHidden.Add(buckled);
                    }
                }
            }
        }

        // Apply Logic:
        // 1. Hide everyone who needs to be hidden
        foreach (var uid in currentHidden)
        {
            SetVisible(uid, false);
        }

        // 2. Reveal everyone who WAS hidden but is NO LONGER hidden (Unbuckled)
        foreach (var uid in _hiddenEntities)
        {
            if (!currentHidden.Contains(uid))
            {
                SetVisible(uid, true);
            }
        }

        // Update cache
        _hiddenEntities.Clear();
        _hiddenEntities.UnionWith(currentHidden);
    }

    private void OnStrapParentChanged(EntityUid uid, StrapComponent component, ref EntParentChangedMessage args)
    {
        // Re-evaluate visibility for all buckled entities when the strap moves (e.g. seat attached to car)
        foreach (var buckled in component.BuckledEntities)
        {
            UpdateVisibility(uid, buckled);
        }
    }

    private void OnStrapped(EntityUid uid, StrapComponent component, ref StrappedEvent args)
    {
        UpdateVisibility(uid, args.Buckle);
    }

    private void OnUnstrapped(EntityUid uid, StrapComponent component, ref UnstrappedEvent args)
    {
        SetVisible(args.Buckle, true);
    }

    private void UpdateVisibility(EntityUid strapUid, EntityUid buckledEntity)
    {
        bool shouldHide = ShouldHide(strapUid);
        SetVisible(buckledEntity, !shouldHide);
    }

    private bool ShouldHide(EntityUid strapUid)
    {
        // Check if this strap is part of an NC Vehicle system (Driver seat or Passenger seat)
        if (TryGetNCVehicle(strapUid, out var ncVehicle))
        {
            return ncVehicle.HidePassengers;
        }
        return false;
    }

    private void SetVisible(EntityUid uid, bool visible)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.Visible = visible;
        }
    }



    private bool TryGetNCVehicle(EntityUid uid, [NotNullWhen(true)] out NCVehicleComponent? component)
    {
        // Case 1: The strap IS the vehicle (Driver)
        if (TryComp(uid, out component))
            return true;

        // Case 2: The strap is a seat attached to the vehicle
        if (TryComp<TransformComponent>(uid, out var xform) && xform.ParentUid.IsValid())
        {
            if (TryComp(xform.ParentUid, out component))
                return true;
        }

        component = null;
        return false;
    }
}
