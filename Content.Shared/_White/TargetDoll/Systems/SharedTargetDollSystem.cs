using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.TargetDoll.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using static Content.Shared.Input.ContentKeyFunctions;

namespace Content.Shared._White.TargetDoll.Systems;

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

    /// <summary>
    /// Selects a specific body provider type for the target doll.
    /// </summary>
    /// <param name="ent">The entity which provider we select.</param>
    /// <param name="provider">The body provider type to select.</param>
    public virtual void SelectProvider(Entity<TargetDollComponent?> ent, BodyProviderType provider)
    {
        if (!TargetDollQuery.Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Selected == provider)
            return;

        ent.Comp.Selected = provider;
        Dirty(ent);
    }

    /// <summary>
    /// Gets a random body provider type from the given body's available providers.
    /// </summary>
    /// <param name="ent">The entity to search within.</param>
    /// <returns>A random body provider type, or <see cref="BodyProviderType.All"/> if no providers are found.</returns>
    public BodyProviderType GetRandomProvider(Entity<BodyComponent?> ent)
    {
        if (!_body.TryGetProviders(ent, out var providers))
            return BodyProviderType.All;

        return _random.PickAndTake(providers).Comp.Type;
    }

    /// <summary>
    /// Gets the currently selected body provider type for the target doll.
    /// </summary>
    /// <param name="ent">The entity which selected provider we want get.</param>
    /// <returns>The selected body provider type, or <see cref="BodyProviderType.All"/> if the component is not found.</returns>
    public BodyProviderType GetSelectedProvider(Entity<TargetDollComponent?> ent)
    {
        if (!TargetDollQuery.Resolve(ent, ref ent.Comp, false))
            return BodyProviderType.All;

        return ent.Comp.Selected;
    }

    #endregion

    #region Private API

    private void HandleProviderSelect(ICommonSession? session, BodyProviderType provider, BodyProviderType? fallbackProvider = null)
    {
        if (session is not { AttachedEntity: { } uid, })
            return;

        if (!TargetDollQuery.TryComp(uid, out var comp))
            return;

        if (comp.Selected != provider)
        {
            SelectProvider((uid, comp), provider);
            return;
        }

        if (fallbackProvider == null)
            return;

        SelectProvider((uid, comp), fallbackProvider.Value);
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed class SelectProviderRequestEvent(BodyProviderType provider) : EntityEventArgs
{
    public BodyProviderType Provider { get; } = provider;
}
