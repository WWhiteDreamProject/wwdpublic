using Content.Shared._White.EntityGenerator;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._White.EntityGenerator;


public sealed class EntityGeneratorSystem : SharedEntityGeneratorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityGeneratorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, EntityGeneratorComponent comp, ExaminedEvent args)
    {
        comp.Charges = GetCurrentCharges(comp, _timing.CurTime);
        var remainingTime = GetRemainingRechargeTime(comp, _timing.CurTime);

        args.PushMarkup(Loc.GetString("entity-generator-examine-charges", ("current", comp.Charges), ("max", comp.MaxCharges)));

        if (comp.Charges >= comp.MaxCharges || !remainingTime.HasValue)
            return;

        args.PushMarkup(Loc.GetString("entity-generator-examine-recharging", ("time", Math.Round(remainingTime.Value.TotalSeconds, 1))));
    }


    protected override void Extract(EntityUid uid, EntityUid user, EntityGeneratorComponent component)
    {
        component.Charges = GetCurrentCharges(component, _timing.CurTime);

        if (component.Charges <= 0 || component.PrototypeId == null)
            return;

        var entity = Spawn(component.PrototypeId, Transform(uid).Coordinates);
        if (!_hands.TryPickupAnyHand(user, entity))
        {
            Del(entity);
            return;
        }

        component.LastExtractTime = _timing.CurTime;
        component.Charges -= 1;

        _popup.PopupEntity(Loc.GetString("entity-generator-extracted"), uid, user);
        Dirty(uid, component);
    }

    private int GetCurrentCharges(EntityGeneratorComponent comp, TimeSpan currentTime)
    {
        if (comp.OnlyFullRecharge)
        {
            return comp.Charges == 0 &&
                currentTime >= comp.LastExtractTime + comp.RechargeDuration
                    ? comp.MaxCharges
                    : comp.Charges;
        }

        var elapsed = currentTime - comp.LastExtractTime;
        var recoveredCharges = (int)(elapsed / comp.RechargeDuration);
        return Math.Min(comp.Charges + recoveredCharges, comp.MaxCharges);
    }

    private TimeSpan? GetRemainingRechargeTime(EntityGeneratorComponent comp, TimeSpan currentTime)
    {
        if (comp.OnlyFullRecharge)
        {
            if (comp.Charges > 0)
                return null;

            var remaining = comp.LastExtractTime + comp.RechargeDuration - currentTime;
            return remaining > TimeSpan.Zero ? remaining : null;
        }

        var nextChargeTime = comp.LastExtractTime + comp.RechargeDuration;
        return nextChargeTime > currentTime ? nextChargeTime - currentTime : null;
    }
}
