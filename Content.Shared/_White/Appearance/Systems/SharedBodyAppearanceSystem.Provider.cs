using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Body.Systems;

namespace Content.Shared._White.Appearance.Systems;

public abstract partial class SharedBodyAppearanceSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyRelayedEvent<ApplyBodyAppearanceDataEvent>>(OnApplyBodyAppearanceData);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyRelayedEvent<GetBodyAppearanceDataEvent>>(OnGetBodyAppearanceData);
    }

    #region Event Handling

    protected virtual void OnGotInserted(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        if (!BodyQuery.HasComp(args.Body))
            return;

        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Body));
    }

    protected virtual void OnGotRemoved(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (!BodyQuery.HasComp(args.Body))
            return;

        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Body));
    }

    private void OnApplyBodyAppearanceData(Entity<BodyAppearanceProviderComponent> ent, ref BodyRelayedEvent<ApplyBodyAppearanceDataEvent> args)
    {
        var relevantData = args.Args.Data;
        if (args.Args.SpecifiedData?.TryGetValue(ent.Comp.Layer, out var specifiedData) == true)
            relevantData = specifiedData;

        if (relevantData is not { } data)
            return;

        SetAppearanceData(ent.AsNullable(), data);
    }

    private void OnGetBodyAppearanceData(Entity<BodyAppearanceProviderComponent> ent, ref BodyRelayedEvent<GetBodyAppearanceDataEvent> args)
    {
        args.Args.Data.Add(ent.Comp.Layer, ent.Comp.Appearance);
    }

    #endregion

    #region Public API

    public virtual void SetAppearanceData(Entity<BodyAppearanceProviderComponent?> ent, BodyAppearanceData appearance)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Appearance == appearance)
            return;

        if (!appearance.ColorGroups.TryGetValue(ent.Comp.Group, out var color))
            return;

        SetColor(ent, color);

        ent.Comp.Appearance = appearance;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Appearance));
    }

    public virtual void SetColor(Entity<BodyAppearanceProviderComponent?> ent, Color color)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Color == color)
            return;

        ent.Comp.Color = color;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Color));
    }

    public virtual void SetPath(Entity<BodyAppearanceProviderComponent?> ent, string path)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Path == path)
            return;

        ent.Comp.Path = path;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Path));
    }

    public virtual void SetState(Entity<BodyAppearanceProviderComponent?> ent, string state)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.State == state)
            return;

        ent.Comp.State = state;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.State));
    }

    #endregion
}
