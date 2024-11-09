using Content.Shared._White.Guns.Stretched;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._White.Guns.Stretched;

public sealed class StretchedVisualizerSystem : VisualizerSystem<StretchedVisualsComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, StretchedVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        _appearance.TryGetData<bool>(uid, StretchedVisuals.Stretched, out var stretched, args.Component);
        _appearance.TryGetData<bool>(uid, AmmoVisuals.HasAmmo, out var hasAmmo, args.Component);

        // StretchedState: Weapon is stretched and ready to fire
        // LoadedState: Weapon is loaded but not stretched
        // UnstrungState: Weapon is neither stretched nor loaded
        args.Sprite.LayerSetState(StretchedVisuals.Layer, stretched ? component.StretchedState : hasAmmo ? component.LoadedState : component.UnstrungState);
    }
}
