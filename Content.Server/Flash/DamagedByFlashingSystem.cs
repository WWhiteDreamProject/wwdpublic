using Content.Server.Flash.Components;
using Content.Shared._White.Damage.Systems;

namespace Content.Server.Flash;

public sealed class DamagedByFlashingSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagedByFlashingComponent, FlashAttemptEvent>(OnFlashAttempt);
    }
    private void OnFlashAttempt(Entity<DamagedByFlashingComponent> ent, ref FlashAttemptEvent args)
    {
        _damageable.ChangeDamage(ent.Owner, ent.Comp.FlashDamage); // WD EDIT

        //TODO: It would be more logical if different flashes had different power,
        //and the damage would be inflicted depending on the strength of the flash.
    }
}
