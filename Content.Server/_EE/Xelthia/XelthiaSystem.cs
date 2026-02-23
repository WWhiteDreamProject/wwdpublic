using Content.Server._White.Body.Bloodstream.Systems;
using Content.Server._White.Body.Systems;
using Content.Shared._EE.Xelthia;
using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Psionics;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Shadowkin;
using Content.Shared.Rejuvenate;
using Content.Shared.Alert;
using Content.Shared.Rounding;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Actions.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Placement;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Content.Shared.Chemistry.Components;


namespace Content.Server._EE.Xelthia;


/// <summary>
/// Xelthia Arm Regrowth stuff
/// </summary>
public sealed class XelthiaSystem : EntitySystem
{
    /// <inheritdoc/>

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

    //public const string XelthiaRegenerateActionId = "XelthiaRegenerateAction";
     public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XelthiaComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<XelthiaComponent, ArmRegrowthEvent>(OnRegrowthAction);
    }

    private void OnInit(EntityUid uid, XelthiaComponent component, ComponentStartup args)
    {
        _actionsSystem.AddAction(uid, ref component.XelthiaRegenerateAction, out var regenerateAction, "XelthiaRegenerateAction");
        if (regenerateAction?.UseDelay != null)
            component.UseDelay = regenerateAction.UseDelay.Value;
    }

    private void OnRegrowthAction(EntityUid uid, XelthiaComponent component, ArmRegrowthEvent args)
    {
        var start = _gameTiming.CurTime;
        var end = _gameTiming.CurTime + component.UseDelay;
        _actionsSystem.SetCooldown(component.XelthiaRegenerateAction!.Value, start, end); // Cooldown

        //IEntityManager entityManager = uid;
        var bodySystem = _entityManager.System<BodySystem>();
        var transformSystem = _entityManager.System<SharedTransformSystem>();
        if (!_entityManager.TryGetComponent(uid, out BodyComponent? body)
            || !_entityManager.TryGetComponent(uid, out TransformComponent? xform))
            return;
        if (bodySystem.TryGetRootBodyPart((uid, body), out var root) || !root.HasValue) // WD EDIT
            return;
        var parts = bodySystem.GetBodyParts((uid, body), BodyPartType.Arm); // Deletes Arms and hands // WD EDIT
        foreach (var part in parts)
        {
            transformSystem.AttachToGridOrMap(part); // WD EDIT
            _entityManager.QueueDeleteEntity(part); // WD EDIT
            _entityManager.SpawnAtPosition("FoodMeatXelthiaTentacle", xform.Coordinates); // Should drop arms equal to the number attached
        }
        // Right Side
        var newLimb = _entityManager.SpawnAtPosition("RightArmXelthia", xform.Coordinates); // Copy-Pasted Code for arm spawning
        if (_entityManager.TryGetComponent(newLimb, out BodyPartComponent? limbComp))
            bodySystem.TryAttachBodyPart(root.Value, (newLimb, limbComp), "right_arm"); // WD EDIT
        var newerLimb = _entityManager.SpawnAtPosition("RightHandXelthia", xform.Coordinates); // Spawns the hand
        Enum.TryParse<BodyPartType>("Hand", out var partType);
        bodySystem.TryCreateBodyPartSlotAndAttachBodyPart(newLimb, newerLimb, "right_hand", partType); // WD EDIT
        // Left Side
        newLimb = _entityManager.SpawnAtPosition("LeftArmXelthia", xform.Coordinates); // Copy-Pasted Code for arm spawning
        if (_entityManager.TryGetComponent(newLimb, out BodyPartComponent? limbComp2))
            bodySystem.TryAttachBodyPart(root.Value, (newLimb, limbComp2), "left_arm"); // WD EDIT
        newerLimb = _entityManager.SpawnAtPosition("LeftHandXelthia", xform.Coordinates); // Spawns the hand
        bodySystem.TryCreateBodyPartSlotAndAttachBodyPart(newLimb, newerLimb, "left_hand", partType); // WD EDIT
        _entityManager.EntitySysManager.GetEntitySystem<SharedAudioSystem>()
            .PlayPvs("/Audio/_EE/Voice/Xelthia/regrow.ogg", uid, null);

        var solution = new Solution();

        TryComp<BloodstreamComponent>(uid, out var stream);
        solution.AddReagent("XelthiaArmJuice", 3.5);
        _bloodstreamSystem.TryAddToBloodstream((uid, stream), solution); // WD EDIT
    }
}
