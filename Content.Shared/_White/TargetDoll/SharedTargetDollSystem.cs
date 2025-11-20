using Content.Shared._White.Body.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using static Content.Shared.Input.ContentKeyFunctions;

namespace Content.Shared._White.TargetDoll;

public abstract class SharedTargetDollSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<SelectBodyPartRequestEvent>(OnSelectBodyPartRequest);

        CommandBinds.Builder
            .Bind(TargetDollHead, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.Head), handle: false, outsidePrediction: false))
            .Bind(TargetDollChest, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.Chest), handle: false, outsidePrediction: false))
            .Bind(TargetDollGroin, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.Groin), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightArm, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.RightArm, BodyPartType.RightHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightHand, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.RightHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftArm, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.LeftArm, BodyPartType.LeftHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftHand, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.LeftHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightLeg, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.RightLeg, BodyPartType.RightFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightFoot, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.RightFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftLeg, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.LeftLeg, BodyPartType.LeftFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftFoot, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.LeftFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollTail, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.Tail), handle: false, outsidePrediction: false))
            .Bind(TargetDollEyes, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.Eyes), handle: false, outsidePrediction: false))
            .Bind(TargetDollMouth, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPartType.Mouth), handle: false, outsidePrediction: false))
            .Register<SharedTargetDollSystem>();
    }

    private void OnSelectBodyPartRequest(SelectBodyPartRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true, } uid
            || !TryComp<TargetDollComponent>(uid, out var targetDoll))
        {
            Log.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(SelectBodyPartRequestEvent)}");
            return;
        }

        SelectBodyPart((uid, targetDoll), msg.BodyPartType);
    }

    private void HandleBodyPartSelect(ICommonSession? session, BodyPartType bodyPartType, BodyPartType? alreadySelectedBodyPart = null)
    {
        if (session is not { AttachedEntity: { } uid, }
            || !TryComp<TargetDollComponent>(uid, out var targetDoll))
            return;

        if (targetDoll.SelectedBodyPartType == bodyPartType)
        {
            if (alreadySelectedBodyPart != null)
                SelectBodyPart((uid, targetDoll), alreadySelectedBodyPart.Value);

            return;
        }

        SelectBodyPart((uid, targetDoll), bodyPartType);
    }

    public virtual void SelectBodyPart(Entity<TargetDollComponent> ent, BodyPartType bodyPartType)
    {
        if (ent.Comp.SelectedBodyPartType == bodyPartType)
            return;

        ent.Comp.SelectedBodyPartType = bodyPartType;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed class SelectBodyPartRequestEvent(BodyPartType bodyPartType) : EntityEventArgs
{
    public BodyPartType BodyPartType { get; } = bodyPartType;
}
