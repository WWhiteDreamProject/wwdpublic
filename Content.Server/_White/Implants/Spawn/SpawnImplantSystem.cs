using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server._White.Implants.Spawn;

public sealed class SpawnImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SubdermalImplantComponent, ActivateSpawnImplantEvent>(OnImplantActivate);
    }

    private void OnImplantActivate(EntityUid uid, SubdermalImplantComponent component, ActivateSpawnImplantEvent args)
    {
        if (!TryComp(uid, out SpawnImplantComponent? implant)
            || !TryComp(component.ImplantedEntity, out TransformComponent? transform))
            return;

        var spear = EntityManager.SpawnEntity(implant.SpawnId, transform.Coordinates);

        if (_hands.TryPickupAnyHand(component.ImplantedEntity.Value, spear))
        {
            _audio.PlayPvs(implant.SoundOnSpawn, spear);
            args.Handled = true;
            return;
        }

        Del(spear);
    }
}
