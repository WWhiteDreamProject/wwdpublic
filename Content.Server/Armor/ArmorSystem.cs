using Content.Server.Cargo.Systems;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared.Armor;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.Armor;

/// <inheritdoc/>
public sealed class ArmorSystem : SharedArmorSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, PriceCalculationEvent>(GetArmorPrice);
    }

    private void GetArmorPrice(EntityUid uid, ArmorComponent component, ref PriceCalculationEvent args)
    {
        foreach (var modifier in component.Modifiers.Coefficients)
        {
            var damageType = _protoManager.Index<DamageTypePrototype>(modifier.Key);
            args.Price += component.PriceMultiplier  * 100 * (1 - modifier.Value);
        }

        foreach (var modifier in component.Modifiers.FlatReduction)
        {
            var damageType = _protoManager.Index<DamageTypePrototype>(modifier.Key);
            args.Price += (component.PriceMultiplier * modifier.Value).Double();
        }
    }
}
