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
            .Bind(TargetDollHead, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.Head), handle: false, outsidePrediction: false))
            .Bind(TargetDollChest, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.Chest), handle: false, outsidePrediction: false))
            .Bind(TargetDollGroin, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.Groin), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightArm, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.RightArm, BodyPart.RightHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightHand, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.RightHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftArm, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.LeftArm, BodyPart.LeftHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftHand, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.LeftHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightLeg, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.RightLeg, BodyPart.RightFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightFoot, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.RightFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftLeg, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.LeftLeg, BodyPart.LeftFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftFoot, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.LeftFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollTail, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.Tail), handle: false, outsidePrediction: false))
            .Bind(TargetDollEyes, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.Eyes), handle: false, outsidePrediction: false))
            .Bind(TargetDollMouth, InputCmdHandler.FromDelegate(session => HandleBodyPartSelect(session, BodyPart.Mouth), handle: false, outsidePrediction: false))
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

        SelectBodyPart((uid, targetDoll), msg.BodyPart);
    }

    private void HandleBodyPartSelect(ICommonSession? session, BodyPart bodyPart, BodyPart? alreadySelectedBodyPart = null)
    {
        if (session is not { AttachedEntity: { } uid, }
            || !TryComp<TargetDollComponent>(uid, out var targetDoll))
            return;

        if (targetDoll.SelectedBodyPart == bodyPart)
        {
            if (alreadySelectedBodyPart != null)
                SelectBodyPart((uid, targetDoll), alreadySelectedBodyPart.Value);

            return;
        }

        SelectBodyPart((uid, targetDoll), bodyPart);
    }

    public virtual void SelectBodyPart(Entity<TargetDollComponent> ent, BodyPart bodyPart)
    {
        if (ent.Comp.SelectedBodyPart == bodyPart)
            return;

        ent.Comp.SelectedBodyPart = bodyPart;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public sealed class SelectBodyPartRequestEvent(BodyPart bodyPart) : EntityEventArgs
{
    public BodyPart BodyPart { get; } = bodyPart;
}
