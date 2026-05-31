using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using static Content.Shared.Input.ContentKeyFunctions;

namespace Content.Shared._White.TargetDoll;

public abstract class SharedTargetDollSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;

    protected EntityQuery<TargetDollComponent> TargetDollQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<SelectProviderRequestEvent>(OnSelectProviderRequest);

        CommandBinds.Builder
            .Bind(TargetDollHead, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.Head), handle: false, outsidePrediction: false))
            .Bind(TargetDollChest, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.Chest), handle: false, outsidePrediction: false))
            .Bind(TargetDollGroin, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.Groin), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightArm, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.RightArm, BodyProviderType.RightHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightHand, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.RightHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftArm, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.LeftArm, BodyProviderType.LeftHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftHand, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.LeftHand), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightLeg, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.RightLeg, BodyProviderType.RightFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollRightFoot, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.RightFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftLeg, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.LeftLeg, BodyProviderType.LeftFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollLeftFoot, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.LeftFoot), handle: false, outsidePrediction: false))
            .Bind(TargetDollTail, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.Tail), handle: false, outsidePrediction: false))
            .Bind(TargetDollEyes, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.Eyes), handle: false, outsidePrediction: false))
            .Bind(TargetDollMouth, InputCmdHandler.FromDelegate(session => HandleProviderSelect(session, BodyProviderType.Mouth), handle: false, outsidePrediction: false))
            .Register<SharedTargetDollSystem>();

        TargetDollQuery = GetEntityQuery<TargetDollComponent>();
    }

    #region Event Handling

    private void OnSelectProviderRequest(SelectProviderRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true, } uid || !TargetDollQuery.TryComp(uid, out var targetDollComp))
        {
            Log.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(SelectProviderRequestEvent)}");
            return;
        }

        SelectProvider((uid, targetDollComp), msg.Provider);
    }

    #endregion

    #region Public API

    public virtual void SelectProvider(Entity<TargetDollComponent?> ent, BodyProviderType provider)
    {
        if (!TargetDollQuery.Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.SelectedProvider == provider)
            return;

        ent.Comp.SelectedProvider = provider;
        Dirty(ent);
    }

    public BodyProviderType GetRandomProvider(Entity<BodyComponent?> body)
    {
        if (!_body.TryGetProviders(body, out var providers))
            return BodyProviderType.All;

        return _random.PickAndTake(providers).Comp.Type;
    }

    public BodyProviderType GetSelectedProvider(Entity<TargetDollComponent?> ent)
    {
        if (!TargetDollQuery.Resolve(ent, ref ent.Comp, false))
            return BodyProviderType.All;

        return ent.Comp.SelectedProvider;
    }

    #endregion

    #region Private API

    private void HandleProviderSelect(ICommonSession? session, BodyProviderType provider, BodyProviderType? alreadySelectedProvider = null)
    {
        if (session is not { AttachedEntity: { } uid, } || !TargetDollQuery.TryComp(uid, out var targetDollComp))
            return;

        if (targetDollComp.SelectedProvider == provider)
        {
            if (alreadySelectedProvider != null)
                SelectProvider((uid, targetDollComp), alreadySelectedProvider.Value);

            return;
        }

        SelectProvider((uid, targetDollComp), provider);
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed class SelectProviderRequestEvent(BodyProviderType provider) : EntityEventArgs
{
    public BodyProviderType Provider { get; } = provider;
}
