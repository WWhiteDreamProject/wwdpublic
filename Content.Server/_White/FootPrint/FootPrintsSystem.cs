using Content.Server.Atmos.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared._White.FootPrint;
using Content.Shared.Standing;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._White.FootPrint;

public sealed class FootPrintsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IMapManager _map = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MobThresholdsComponent> _mobThresholdQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<LayingDownComponent> _layingQuery;

   public override void Initialize()
   {
       base.Initialize();

       _transformQuery = GetEntityQuery<TransformComponent>();
       _mobThresholdQuery = GetEntityQuery<MobThresholdsComponent>();
       _appearanceQuery = GetEntityQuery<AppearanceComponent>();
       _layingQuery = GetEntityQuery<LayingDownComponent>();

       SubscribeLocalEvent<FootPrintsComponent, ComponentStartup>(OnStartupComponent);
       SubscribeLocalEvent<FootPrintsComponent, MoveEvent>(OnMove);
   }

    private void OnStartupComponent(EntityUid uid, FootPrintsComponent comp, ComponentStartup args)
    {
        comp.StepSize += _random.NextFloat(-0.05f, 0.05f);
    }

    private void OnMove(EntityUid uid, FootPrintsComponent comp, ref MoveEvent args)
    {
        if (comp.PrintsColor.A <= 0f
            || !_transformQuery.TryComp(uid, out var transform)
            || !_mobThresholdQuery.TryComp(uid, out var mobThreshHolds)
            || !_map.TryFindGridAt(_transform.GetMapCoordinates((uid, transform)), out var gridUid, out _))
            return;

        var dragging = mobThreshHolds.CurrentThresholdState is MobState.Critical or MobState.Dead || _layingQuery.TryComp(uid, out var laying) && laying.IsCrawlingUnder;
        var distance = (transform.LocalPosition - comp.StepPos).Length();
        var stepSize = dragging ? comp.DragSize : comp.StepSize;

        if (!(distance > stepSize))
            return;

        comp.RightStep = !comp.RightStep;

        var entity = Spawn(comp.StepProtoId, CalcCoords(gridUid, comp, transform, dragging));
        var footPrintComponent = Comp<FootPrintComponent>(entity); // There's NO way there's no footprint commponent in a FOOTPRINT

        footPrintComponent.PrintOwner = uid;
        Dirty(entity, footPrintComponent);

        if (_appearanceQuery.TryComp(entity, out var appearance))
        {
            _appearance.SetData(entity, FootPrintVisualState.State, PickState(uid, dragging), appearance);
            _appearance.SetData(entity, FootPrintVisualState.Color, comp.PrintsColor, appearance);
        }

        if (!_transformQuery.TryComp(entity, out var stepTransform))
            return;

        stepTransform.LocalRotation = dragging
            ? (transform.LocalPosition - comp.StepPos).ToAngle() + Angle.FromDegrees(-90f)
            : transform.LocalRotation + Angle.FromDegrees(180f);

        comp.PrintsColor = comp.PrintsColor.WithAlpha(ReduceAlpha(comp.PrintsColor.A, comp.ColorReduceAlpha));
        comp.StepPos = transform.LocalPosition;

        if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutionContainer)
            || !_solution.ResolveSolution((entity, solutionContainer), footPrintComponent.SolutionName, ref footPrintComponent.Solution, out var solution)
            || string.IsNullOrWhiteSpace(comp.ReagentToTransfer) || solution.Volume >= 1)
            return;

        _solution.TryAddReagent(footPrintComponent.Solution.Value, comp.ReagentToTransfer, 1, out _);
    }

    private EntityCoordinates CalcCoords(EntityUid uid, FootPrintsComponent comp, TransformComponent transform, bool state)
    {
        if (state)
            return new EntityCoordinates(uid, transform.LocalPosition);

        var offset = comp.RightStep
            ? new Angle(Angle.FromDegrees(180f) + transform.LocalRotation).RotateVec(comp.OffsetPrint)
            : new Angle(transform.LocalRotation).RotateVec(comp.OffsetPrint);

        return new EntityCoordinates(uid, transform.LocalPosition + offset);
    }

    private FootPrintVisuals PickState(EntityUid uid, bool dragging)
    {
        var state = FootPrintVisuals.BareFootPrint;

        if (_inventory.TryGetSlotEntity(uid, "shoes", out _))
            state = FootPrintVisuals.ShoesPrint;

        if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var suit) && TryComp<PressureProtectionComponent>(suit, out _))
            state = FootPrintVisuals.SuitPrint;

        if (dragging)
            state = FootPrintVisuals.Dragging;

        return state;
    }

    private float ReduceAlpha(float alpha, float reductionAmount)
    {
        if (alpha - reductionAmount > 0f)
            alpha -= reductionAmount;
        else
            alpha = 0f;

        return alpha;
    }
}
