using Content.Shared._NC.Netrunning.Components;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Stunnable;

using Content.Server.Electrocution;

namespace Content.Server._NC.Netrunning.Systems;

public sealed class QuickhackSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;

    public void ApplyEffect(EntityUid target, NetProgramComponent program)
    {
        switch (program.QuickhackType)
        {
            case QuickhackType.WeaponDrop:
                _hands.TryDrop(target);
                break;

            case QuickhackType.Damage:
                var damageSpec = new DamageSpecifier();
                var damageType = "Shock";

                // Validate if Shock exists, otherwise fallback to Blunt
                if (!_proto.HasIndex<DamageTypePrototype>(damageType))
                    damageType = "Blunt";

                if (_proto.HasIndex<DamageTypePrototype>(damageType))
                {
                    damageSpec.DamageDict.Add(damageType, program.Damage);
                    _damageable.TryChangeDamage(target, damageSpec);
                }

                // Electrocution if stunDuration is set
                if (program.StunDuration > 0)
                {
                    // TryDoElectrocution(uid, sourceUid, damage, duration, refresh, ignoreInsulation)
                    _electrocution.TryDoElectrocution(target, null, program.Damage, TimeSpan.FromSeconds(program.StunDuration), true, ignoreInsulation: true);
                }
                break;

            case QuickhackType.Blind:
                EnsureComp<BlindableComponent>(target);
                _status.TryAddStatusEffect(target, "TemporaryBlindness", TimeSpan.FromSeconds(program.Duration), true, "TemporaryBlindness");
                break;
        }
    }
}
