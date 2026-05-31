using Content.Shared._White.Layer.Systems;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Content.Shared.Humanoid;

namespace Content.Shared.DeltaV.Harpy;

public sealed class HarpyVisualsSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedHideableLayersSystem _hideableLayers = default!; // WD EDIT

    [ValidatePrototypeId<TagPrototype>]
    private const string HarpyWingsTag = "HidesHarpyWings";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Traits.Assorted.Components.SingerComponent, DidEquipEvent>(OnDidEquipEvent);
        SubscribeLocalEvent<Traits.Assorted.Components.SingerComponent, DidUnequipEvent>(OnDidUnequipEvent);
    }

    private void OnDidEquipEvent(EntityUid uid, Traits.Assorted.Components.SingerComponent component, DidEquipEvent args)
    {
        if (args.Slot == "outerClothing" && _tagSystem.HasTag(args.Equipment, HarpyWingsTag))
        {
            _hideableLayers.SetLayerOcclusion(uid, HumanoidVisualLayers.RArm, false, args.SlotFlags);
            _hideableLayers.SetLayerOcclusion(uid, HumanoidVisualLayers.Tail, false, args.SlotFlags);
        }
    }

    private void OnDidUnequipEvent(EntityUid uid, Traits.Assorted.Components.SingerComponent component, DidUnequipEvent args)
    {
        if (args.Slot == "outerClothing" && _tagSystem.HasTag(args.Equipment, HarpyWingsTag))
        {
            _hideableLayers.SetLayerOcclusion(uid, HumanoidVisualLayers.RArm, true, args.SlotFlags);
            _hideableLayers.SetLayerOcclusion(uid, HumanoidVisualLayers.Tail, true, args.SlotFlags);
        }
    }
}
