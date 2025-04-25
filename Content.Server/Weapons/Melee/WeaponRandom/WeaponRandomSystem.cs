using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Weapons.Melee.WeaponRandom;

/// <summary>
/// This adds a random damage bonus to melee attacks or throws based on damage bonus amount and probability.
/// </summary>
public sealed class WeaponRandomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponRandomComponent, MeleeHitEvent>(OnMeleeHit);
        //WWDP EDIT START
        SubscribeLocalEvent<WeaponRandomComponent, GetThrowingDamageEvent>(OnThrowDamageBonusCheck);
        SubscribeLocalEvent<WeaponRandomComponent, ThrowDoHitEvent>(OnThrowDoHit, before: [typeof(SharedDamageOtherOnHitSystem)]);
        //WWDP EDIT END
    }
    /// <summary>
    /// On Melee hit there is a possible chance of additional bonus damage occuring.
    /// </summary>
    private void OnMeleeHit(EntityUid uid, WeaponRandomComponent component, MeleeHitEvent args)
    {
        //WWDP EDIT START
        if (component.ApplyBonusOnThrow)
            return;
        //WWDP EDIT END

        if (_random.Prob(component.RandomDamageChance))
        {
            _audio.PlayPvs(component.DamageSound, uid);
            args.BonusDamage = component.DamageBonus;
        }
    }

    //WWDP EDIT START
    private void OnThrowDoHit(EntityUid uid, WeaponRandomComponent component, ref ThrowDoHitEvent args)
    {
        if (!component.ApplyBonusOnThrow
            || component.ForcedThrowTargetPart == null
            || !_random.Prob(component.RandomDamageChance))
            return;

        component.IsCriticalThrow = true;
        _audio.PlayPvs(component.DamageSound, args.Target);
        args.TargetPart = component.ForcedThrowTargetPart.Value;
    }

    private void OnThrowDamageBonusCheck(EntityUid uid, WeaponRandomComponent component, ref GetThrowingDamageEvent args)
    {
        if (args.IsExaminingDamage || !component.IsCriticalThrow)
            return;

        args.Damage += component.DamageBonus;
        component.IsCriticalThrow = false;
    }
    //WWDP EDIT END
}
