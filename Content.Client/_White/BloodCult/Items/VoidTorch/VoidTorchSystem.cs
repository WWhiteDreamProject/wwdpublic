using Content.Client.Light.Components;
using Content.Shared._White.BloodCult;
using Content.Shared._White.BloodCult.Items.VoidTorch;
using Robust.Client.GameObjects;

namespace Content.Client._White.BloodCult.Items.VoidTorch;

public sealed class VoidTorchSystem : VisualizerSystem<VoidTorchComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        VoidTorchComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!AppearanceSystem.TryGetData<bool>(uid, GenericCultVisuals.State, out var state)
            || !TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
            return;

        lightBehaviour.StopLightBehaviour();
        lightBehaviour.StartLightBehaviour(state ? component.TurnOnLightBehaviour : component.TurnOffLightBehaviour);
    }
}
