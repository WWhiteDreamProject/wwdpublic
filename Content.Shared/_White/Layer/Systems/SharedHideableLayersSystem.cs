using System.Numerics;
using Content.Shared._White.Layer.Components;
using Content.Shared.Inventory;
using Robust.Shared.Utility;

namespace Content.Shared._White.Layer.Systems;

public abstract class SharedHideableLayersSystem : EntitySystem
{
    protected EntityQuery<HideableLayersComponent> HideableLayersQuery;

    public override void Initialize()
    {
        base.Initialize();

        HideableLayersQuery = GetEntityQuery<HideableLayersComponent>();
    }

    #region Public API

    /// <summary>
    /// Toggles a sprite layer visibility.
    /// </summary>
    /// <param name="ent">The hideable layers entity</param>
    /// <param name="layer">The Layer to toggle visibility for.</param>
    /// <param name="hidden">Whether to hide or show the layer. If more than once piece of clothing is hiding the layer, it may remain hidden.</param>
    /// <param name="slot">Equipment slot that has the clothing that is (or was) hiding the layer.</param>
    public virtual void SetLayerOcclusion(Entity<HideableLayersComponent?> ent, Enum layer, bool hidden, SlotFlags slot)
    {
        if (!HideableLayersQuery.Resolve(ent, ref ent.Comp))
            return;

        #if DEBUG
        DebugTools.AssertNotEqual(slot, SlotFlags.NONE);
        // Check that only a single bit in the bitflag is set
        var powerOfTwo = BitOperations.RoundUpToPowerOf2((uint)slot);
        DebugTools.AssertEqual((uint)slot, powerOfTwo);
        #endif

        var dirty = false;
        if (hidden)
        {
            var oldSlots = ent.Comp.HiddenLayers.GetValueOrDefault(layer);
            ent.Comp.HiddenLayers[layer] = slot | oldSlots;
            dirty |= (oldSlots & slot) != slot;
        }
        else if (ent.Comp.HiddenLayers.TryGetValue(layer, out var oldSlots))
        {
            ent.Comp.HiddenLayers[layer] = ~slot & oldSlots;
            if (ent.Comp.HiddenLayers[layer] == SlotFlags.NONE)
                ent.Comp.HiddenLayers.Remove(layer);

            dirty |= (oldSlots & slot) != 0;
        }

        if (!dirty)
            return;

        Dirty(ent);

        var ev = new HideableLayerVisibilityChangedEvent(layer, ent.Comp.HiddenLayers.ContainsKey(layer));
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}

/// <summary>
/// Event raised on a hideable layers entity when one of its layers changes its visibility.
/// </summary>
[ByRefEvent]
public readonly record struct HideableLayerVisibilityChangedEvent(Enum Layer, bool Visible);
