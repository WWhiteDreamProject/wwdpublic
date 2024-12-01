using Content.Server.Holosign;
using Content.Shared.Destructible;

namespace Content.Server._White.Holosign;

public sealed class HolosignSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HolosignComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnDestruction(EntityUid uid, HolosignComponent component, DestructionEventArgs args)
    {
        if (!TryComp<HolosignProjectorComponent>(component.Projector, out var holosignProjector))
            return;

        holosignProjector.Signs.Remove(uid);
        ++holosignProjector.Uses;
    }
}
