using Content.Shared._White.Guns;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Shared.Player;

namespace Content.Client._White.Guns;
public sealed class GunOverheatSystem : SharedGunFluxSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FluxCoreComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (comp.EffectSteps == 0 ||
                !sprite.LayerMapTryGet(FluxCoreVisualLayers.Effect, out var layerIndex) ||
                !sprite.TryGetLayer(layerIndex, out var layer, true))
                continue;

            var fraction = GetCurrentFlux(comp) / comp.Capacity;
            int stateNum = (int)MathF.Round(fraction * comp.EffectSteps);
            var state = $"{comp.EffectState}-{stateNum}";

            // i have no idea what is happening inside RebuildBounds(), but it looks vaguely expensive
            if (layer.State.Name == state)
                continue;

            sprite.LayerSetState(layerIndex, state);
        }
    }
}

public enum FluxCoreVisualLayers { Base, Effect }
