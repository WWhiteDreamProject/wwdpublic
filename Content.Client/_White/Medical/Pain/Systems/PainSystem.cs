using Content.Client.Alerts;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Pain.Components;
using Content.Shared._White.Medical.Pain.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._White.Medical.Pain.Systems;

public sealed class PainSystem : SharedPainSystem
{
    [Dependency] private readonly SpriteSystem _system = default!;

    public override void Initialize()
    {
       base.Initialize();

       SubscribeLocalEvent<PainfulComponent, ComponentHandleState>(OnHandleState);

       SubscribeLocalEvent<PainThresholdsComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    #region Event Handling

    private void OnHandleState(Entity<PainfulComponent> painful, ref ComponentHandleState args)
    {
        if (args.Current is not PainfulComponentState state)
            return;

        var oldPain = painful.Comp.CurrentPain;

        painful.Comp.Pain = state.Pain;
        painful.Comp.PainMultiplier = state.PainMultiplier;
        painful.Comp.LastUpdate = state.LastUpdate;

        if (oldPain != painful.Comp.CurrentPain)
            RaiseLocalEvent(painful, new AfterPainChangedEvent(painful, painful.Comp.CurrentPain, oldPain), true);
    }

    private void OnUpdateAlertSprite(Entity<PainThresholdsComponent> painThresholds, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != painThresholds.Comp.BodyStatusAlert)
            return;

        var sprite = args.SpriteViewEnt.AsNullable();

        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.Head, GetState(painThresholds, BodyPartType.Head));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.Chest, GetState(painThresholds, BodyPartType.Chest));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.Groin, GetState(painThresholds, BodyPartType.Groin));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.LeftArm, GetState(painThresholds, BodyPartType.LeftArm));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.LeftHand, GetState(painThresholds, BodyPartType.LeftHand));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.RightArm, GetState(painThresholds, BodyPartType.RightArm));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.RightHand, GetState(painThresholds, BodyPartType.RightHand));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.LeftLeg, GetState(painThresholds, BodyPartType.LeftLeg));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.LeftFoot, GetState(painThresholds, BodyPartType.LeftFoot));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.RightLeg, GetState(painThresholds, BodyPartType.RightLeg));
        _system.LayerSetRsiState(sprite, BodyStatusVisualLayers.RightFoot, GetState(painThresholds, BodyPartType.RightFoot));
    }

    private string GetState(Entity<PainThresholdsComponent> painThresholds, BodyPartType bodyPartType) =>
        $"{bodyPartType.ToString().ToLower()}_{painThresholds.Comp.PainStatus[bodyPartType].ToString().ToLower()}";

    #endregion
}
