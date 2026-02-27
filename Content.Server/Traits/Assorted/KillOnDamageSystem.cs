using Content.Shared._White.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;

namespace Content.Server.Traits.Assorted;

public sealed class KillOnDamageSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KillOnDamageComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, KillOnDamageComponent component, DamageChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobState))
            return;

        if (!_mob.IsDead(uid) && args.Damage.DamageDict.TryGetValue(component.DamageType, out FixedPoint2 value) && value >= component.Threshold) // WD EDIT
        {
            var popup = Loc.GetString(component.Popup, ("name", Identity.Name(uid, EntityManager)));
            _popup.PopupEntity(popup, uid, PopupType.LargeCaution);
            _mob.ChangeMobState(uid, MobState.Dead, mobState);
        }
    }
}
