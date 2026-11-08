using Content.Shared._White.Damage;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared._White.Damage;

[TestFixture]
[TestOf(typeof(DamageGroupPrototype))]
[TestOf(typeof(DamageSpecifier))]
public sealed class DamageTest : ContentUnitTest
{
    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> Caustic = "Caustic";
    private static readonly ProtoId<DamageTypePrototype> Cellular = "Cellular";
    private static readonly ProtoId<DamageTypePrototype> Cold = "Cold";
    private static readonly ProtoId<DamageTypePrototype> Heat = "Heat";
    private static readonly ProtoId<DamageTypePrototype> Piercing = "Piercing";
    private static readonly ProtoId<DamageTypePrototype> Poison = "Poison";
    private static readonly ProtoId<DamageTypePrototype> Radiation = "Radiation";
    private static readonly ProtoId<DamageTypePrototype> Shock = "Shock";
    private static readonly ProtoId<DamageTypePrototype> Slash = "Slash";

    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";

    private static readonly string Prototypes = $"""
        - type: damageType
          id: {Asphyxiation}
          name: damage-type-asphyxiation

        - type: damageType
          id: {Blunt}
          name: damage-type-blunt

        - type: damageType
          id: {Caustic}
          name: damage-type-caustic

        - type: damageType
          id: {Cellular}
          name: damage-type-cellular

        - type: damageType
          id: {Cold}
          name: damage-type-cold
          wound: WoundCold

        - type: damageType
          id: {Heat}
          name: damage-type-heat

        - type: damageType
          id: {Piercing}
          name: damage-type-piercing

        - type: damageType
          id: {Poison}
          name: damage-type-poison

        - type: damageType
          id: {Radiation}
          name: damage-type-radiation

        - type: damageType
          id: {Shock}
          name: damage-type-shock

        - type: damageType
          id: {Slash}
          name: damage-type-slash

        - type: damageGroup
          id: {Brute}
          name: damage-group-brute
          types:
          - {Blunt}
          - {Piercing}
          - {Slash}
        """;

    private IPrototypeManager _prototype;

    /// <summary>
    /// Checks that DamageSpecifier correctly stores and calculates damage values across multiple damage types,
    /// handles multiplication and division operations, and properly distributes damage across damage groups.
    /// </summary>
    [Test]
    public void TestDamageSpecifier()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        _prototype = IoCManager.Resolve<IPrototypeManager>();
        _prototype.Initialize();
        _prototype.LoadString(Prototypes);
        _prototype.ResolveResults();

        var damage = new DamageSpecifier
        {
            { Asphyxiation, 1 },
            { Blunt, 1 },
            { Caustic, 1 },
            { Cellular, 1 },
            { Cold, 1 },
            { Heat, 1 },
            { Piercing, 1 },
            { Poison, 1 },
            { Radiation, 1 },
            { Shock, 1 },
            { Slash, 1 }
        };

        var totalDamage = FixedPoint2.New(11);
        var damagePerType = FixedPoint2.New(1);

        ChekDamage(damage, totalDamage,  damagePerType);

        var multiple = 2.5f;
        damage *= multiple;
        totalDamage *= multiple;
        damagePerType *= multiple;

        ChekDamage(damage, totalDamage,  damagePerType);

        damage /= multiple;
        totalDamage /= multiple;
        damagePerType /= multiple;

        ChekDamage(damage, totalDamage,  damagePerType);

        var brute = _prototype.Index(Brute);
        totalDamage = damagePerType * brute.Types.Count;
        damage = new(brute, totalDamage);

        Assert.That(damage.GetTotal(), Is.EqualTo(totalDamage));
        Assert.That(damage[Blunt], Is.EqualTo(damagePerType));
        Assert.That(damage[Piercing], Is.EqualTo(damagePerType));
        Assert.That(damage[Slash], Is.EqualTo(damagePerType));
    }

    private void ChekDamage(DamageSpecifier damage, FixedPoint2 totalDamage, FixedPoint2 damagePerType)
    {
        Assert.That(damage.GetTotal(), Is.EqualTo(totalDamage));
        Assert.That(damage[Asphyxiation], Is.EqualTo(damagePerType));
        Assert.That(damage[Blunt], Is.EqualTo(damagePerType));
        Assert.That(damage[Caustic], Is.EqualTo(damagePerType));
        Assert.That(damage[Cellular], Is.EqualTo(damagePerType));
        Assert.That(damage[Cold], Is.EqualTo(damagePerType));
        Assert.That(damage[Heat], Is.EqualTo(damagePerType));
        Assert.That(damage[Piercing], Is.EqualTo(damagePerType));
        Assert.That(damage[Poison], Is.EqualTo(damagePerType));
        Assert.That(damage[Radiation], Is.EqualTo(damagePerType));
        Assert.That(damage[Shock], Is.EqualTo(damagePerType));
        Assert.That(damage[Slash], Is.EqualTo(damagePerType));
    }
}
