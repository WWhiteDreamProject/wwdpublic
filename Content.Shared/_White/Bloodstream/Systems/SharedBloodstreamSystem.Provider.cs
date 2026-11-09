using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;

namespace Content.Shared._White.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<BloodstreamProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<BloodstreamProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<BloodstreamProviderComponent, BodyRelayedEvent<GetBleedingEvent>>(OnGetBleeding);
    }

    #region Event Handling

    private void OnGotInserted(Entity<BloodstreamProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(BloodstreamProviderComponent.Body));

        RaiseLocalEvent(args.Body, new BleedingLevelChangedEvent(ent.Comp.Level, ent.Comp.Location));
    }

    private void OnGotRemoved(Entity<BloodstreamProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(BloodstreamProviderComponent.Body));

        RaiseLocalEvent(args.Body, new BleedingLevelChangedEvent(BleedingLevel.None, ent.Comp.Location));
    }

    private void OnGetBleeding(Entity<BloodstreamProviderComponent> ent, ref BodyRelayedEvent<GetBleedingEvent> args)
    {
        var getBleedingEv = new GetBleedingEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref getBleedingEv);

        SetBleeding(ent.AsNullable(), getBleedingEv.Bleeding);

        args.Args = new (args.Args.Bleeding + ent.Comp.Bleeding);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Updates the bleeding amount for the given bloodstream provider.
    /// </summary>
    public void SetBleeding(Entity<BloodstreamProviderComponent?> ent, FixedPoint2 bleeding)
    {
        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Bleeding == bleeding)
            return;

        ent.Comp.Bleeding = bleeding;
        DirtyField(ent, ent.Comp, nameof(BloodstreamProviderComponent.Bleeding));

        UpdateBleedingLevel((ent, ent.Comp));
    }

    #endregion

    #region Private API

    public void UpdateBleedingLevel(Entity<BloodstreamProviderComponent> ent)
    {
        var bleedingLevel = ent.Comp.Thresholds.HighestMatch(ent.Comp.Bleeding) ?? BleedingLevel.Zero;
        if (ent.Comp.Level == bleedingLevel)
            return;

        ent.Comp.Level = bleedingLevel;
        DirtyField(ent, ent.Comp, nameof(BloodstreamProviderComponent.Level));

        if (ent.Comp.Body is not { } body)
            return;

        RaiseLocalEvent(body, new BleedingLevelChangedEvent(bleedingLevel, ent.Comp.Location));
    }

    #endregion
}
