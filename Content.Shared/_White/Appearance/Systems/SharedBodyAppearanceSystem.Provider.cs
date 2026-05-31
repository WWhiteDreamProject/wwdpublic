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
        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Body));
    }

    protected virtual void OnGotRemoved(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Body));
    }

    private void OnApplyBodyAppearanceData(Entity<BodyAppearanceProviderComponent> ent, ref BodyRelayedEvent<ApplyBodyAppearanceDataEvent> args)
    {
        var relevantData = args.Args.Data;
        if (args.Args.SpecifiedData?.TryGetValue(args.Provider.Type, out var specifiedData) == true)
            relevantData = specifiedData;

        if (relevantData is not { } data)
            return;

        SetAppearanceData(ent.AsNullable(), data);
    }

    private void OnGetBodyAppearanceData(Entity<BodyAppearanceProviderComponent> ent, ref BodyRelayedEvent<GetBodyAppearanceDataEvent> args)
    {
        args.Args.Data.Add(args.Provider.Type, ent.Comp.Appearance);
    }

    #endregion

    #region Public API

    public virtual void SetAppearanceData(Entity<BodyAppearanceProviderComponent?> ent, BodyAppearanceData appearance)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Appearance == appearance)
            return;

        ent.Comp.Appearance = appearance;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Appearance));
    }

    public virtual void SetColor(Entity<BodyAppearanceProviderComponent?> ent, Color color)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Data.Color == color)
            return;

        ent.Comp.Data.Color = color;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Data));
    }

    public virtual void SetLayerData(Entity<BodyAppearanceProviderComponent?> ent, PrototypeLayerData data)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Data == data)
            return;

        ent.Comp.Data = data;
        DirtyField(ent, ent.Comp, nameof(BodyAppearanceProviderComponent.Data));
    }

    #endregion
}
