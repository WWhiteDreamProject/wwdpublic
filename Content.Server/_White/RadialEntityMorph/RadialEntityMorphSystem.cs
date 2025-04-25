using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.RadialSelector;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server._White.RadialEntityMorph;

public sealed class RadialEntityMorphSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadialEntityMorphComponent, BeforeActivatableUIOpenEvent>(OnUseInHand);
        SubscribeLocalEvent<RadialEntityMorphComponent, RadialSelectorSelectedMessage>(OnPrototypeSelected);
    }

    private void OnUseInHand(Entity<RadialEntityMorphComponent> entity, ref BeforeActivatableUIOpenEvent args)
    {
        if (!_ui.HasUi(entity.Owner, RadialSelectorUiKey.Key))
            return;

        _ui.SetUiState(entity.Owner, RadialSelectorUiKey.Key, new RadialSelectorState(entity.Comp.Entries));
    }

    private void OnPrototypeSelected(EntityUid uid, RadialEntityMorphComponent component, RadialSelectorSelectedMessage args)
    {
        if (args.UiKey is not RadialSelectorUiKey.Key)
            return;

        if (!TryComp(args.Actor, out HandsComponent? hands))
            return;

        foreach (var hand in hands.Hands.Values)
        {
            if (hand.HeldEntity != uid)
                continue;

            Del(uid);

            var newItem = Spawn(args.SelectedItem, Transform(args.Actor).Coordinates);
            _hands.TryPickup(args.Actor, newItem, hand.Name, handsComp: hands);

            _ui.CloseUi(uid, RadialSelectorUiKey.Key);
            return;
        }
    }
}
