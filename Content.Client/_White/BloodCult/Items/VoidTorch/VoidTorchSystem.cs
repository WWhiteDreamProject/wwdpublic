using Content.Client.Light.Components;
using Content.Client.Light.EntitySystems;
using Content.Shared._White.BloodCult;
using Content.Shared._White.BloodCult.Items.VoidTorch;
using Robust.Client.GameObjects;

namespace Content.Client._White.BloodCult.Items.VoidTorch;

public sealed class VoidTorchSystem : VisualizerSystem<VoidTorchComponent>
{
    [Dependency] private readonly LightBehaviorSystem _lightBehavior = default!;

    protected override void OnAppearanceChange(EntityUid uid, VoidTorchComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, GenericCultVisuals.State, out var state)
            || !TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
            return;

        _lightBehavior.StopLightBehaviour((uid, lightBehaviour));
        _lightBehavior.StartLightBehaviour((uid, lightBehaviour), state ? component.TurnOnLightBehaviour : component.TurnOffLightBehaviour);
    }
}
