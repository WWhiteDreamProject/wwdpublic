using Content.Shared._White.Body.Systems;
using Content.Shared._White.Pain.Components;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Pain.Systems;

public abstract partial class SharedPainfulSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<PainfulProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<PainfulProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<PainfulProviderComponent, BodyRelayedEvent<GetPainEvent>>(OnGetPain);
    }

    #region Event Handling

    private void OnGotInserted(Entity<PainfulProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(PainfulProviderComponent.Body));

        RaiseLocalEvent(args.Body, new PainLevelChangedEvent(ent.Comp.Level, ent.Comp.Location));
    }

    private void OnGotRemoved(Entity<PainfulProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(PainfulProviderComponent.Body));

        RaiseLocalEvent(args.Body, new PainLevelChangedEvent(PainLevel.None, ent.Comp.Location));
    }

    private void OnGetPain(Entity<PainfulProviderComponent> ent, ref BodyRelayedEvent<GetPainEvent> args)
    {
        var getPainEv = new GetPainEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref getPainEv);

        SetPain(ent.AsNullable(), getPainEv.Pain);

        args.Args = new (args.Args.Pain + ent.Comp.Pain);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Updates the pain amount for the given painful provider.
    /// </summary>
    public void SetPain(Entity<PainfulProviderComponent?> ent, FixedPoint2 pain)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Pain == pain)
            return;

        ent.Comp.Pain = pain;
        DirtyField(ent, ent.Comp, nameof(PainfulProviderComponent.Pain));

        UpdatePainLevel((ent, ent.Comp));
    }

    #endregion

    #region Private API

    public void UpdatePainLevel(Entity<PainfulProviderComponent> ent)
    {
        var painLevel = ent.Comp.Thresholds.HighestMatch(ent.Comp.Pain) ?? PainLevel.Zero;
        if (ent.Comp.Level == painLevel)
            return;

        ent.Comp.Level = painLevel;
        DirtyField(ent, ent.Comp, nameof(PainfulProviderComponent.Level));

        if (ent.Comp.Body is not { } body)
            return;

        RaiseLocalEvent(body, new PainLevelChangedEvent(painLevel, ent.Comp.Location));
    }

    #endregion
}
