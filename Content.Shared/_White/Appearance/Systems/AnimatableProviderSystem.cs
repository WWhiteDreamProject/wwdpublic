using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared.Actions;

namespace Content.Shared._White.Appearance.Systems;

public sealed class AnimatableProviderSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBodyAppearanceSystem _bodyAppearance = default!;

    private EntityQuery<BodyAppearanceProviderComponent> _appearanceQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatableProviderComponent, AnimatableProviderActionEvent>(OnAction);
        SubscribeLocalEvent<AnimatableProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<AnimatableProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);

        _appearanceQuery = GetEntityQuery<BodyAppearanceProviderComponent>();
    }

    #region Event Handling

    private void OnAction(Entity<AnimatableProviderComponent> ent, ref AnimatableProviderActionEvent args)
    {
        if (!_appearanceQuery.TryComp(ent, out var appearanceComp))
            return;

        ent.Comp.Wagging = !ent.Comp.Wagging;
        DirtyField(ent, ent.Comp, nameof(AnimatableProviderComponent.Wagging));

        var entries = new List<BodyAppearanceProviderEntry>();
        foreach (var entry in appearanceComp.Entries)
        {
            if (ent.Comp.Wagging)
            {
                entries.Add(entry.WithState($"{entry.State}{ent.Comp.Suffix}"));
                continue;
            }

            var suffixIndex = entry.State.IndexOf(ent.Comp.Suffix, StringComparison.Ordinal);
            if (suffixIndex < 0)
            {
                entries.Add(entry);
                continue;
            }

            entries.Add(entry.WithState(entry.State.Remove(suffixIndex, ent.Comp.Suffix.Length)));
        }

        _bodyAppearance.SetEntries((ent, appearanceComp), entries);
    }

    private void OnGotInserted(Entity<AnimatableProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        if (!_actionContainer.EnsureAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action))
            return;

        _actions.AddAction(args.Body, ent.Comp.ActionEntity.Value, ent);
    }

    private void OnGotRemoved(Entity<AnimatableProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        _actions.RemoveAction(args.Body, ent.Comp.ActionEntity);
    }

    #endregion
}

public sealed partial class AnimatableProviderActionEvent : InstantActionEvent;
