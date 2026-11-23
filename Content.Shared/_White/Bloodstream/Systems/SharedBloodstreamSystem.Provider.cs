using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Helpers;
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

        var ev = new BleedingLevelChangedEvent(ent.Comp.Level, ent.Comp.Location);
        RaiseLocalEvent(args.Body, ref ev);

        UpdateBleeding(args.Body.Owner);
    }

    private void OnGotRemoved(Entity<BloodstreamProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(BloodstreamProviderComponent.Body));

        var ev = new BleedingLevelChangedEvent(BleedingLevel.None, ent.Comp.Location);
        RaiseLocalEvent(args.Body, ref ev);

        UpdateBleeding(args.Body.Owner);
    }

    private void OnGetBleeding(Entity<BloodstreamProviderComponent> ent, ref BodyRelayedEvent<GetBleedingEvent> args)
    {
        var ev = new GetBleedingEvent(FixedPoint2.Zero);
        RaiseLocalEvent(ent, ref ev);

        SetBleeding(ent.AsNullable(), ev.Bleeding);

        args.Args = args.Args.Add(ev);
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

        var ev = new BleedingLevelChangedEvent(bleedingLevel, ent.Comp.Location);
        RaiseLocalEvent(body, ref ev);
    }

    #endregion
}
