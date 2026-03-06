using System.Numerics;
using Content.Server._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Wounds.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._White.Body.Bloodstream.Systems;

public sealed partial class BloodstreamSystem
{
    private void InitializeWound() =>
        SubscribeLocalEvent<BloodSplatterWoundComponent, WoundDamageChangedEvent>(OnWoundDamageChange);

    #region Event Handling

    private void OnWoundDamageChange(Entity<BloodSplatterWoundComponent> ent, ref WoundDamageChangedEvent args)
    {
        if (args.Wound.Comp.Body is not {} body || args.Origin is not {} origin)
            return;

        var damageDealt = args.Wound.Comp.Damage - args.OldDamage;
        if (ent.Comp.MinDamageThreshold > damageDealt)
            return;

        var chance = ((damageDealt - ent.Comp.MinDamageThreshold) / (ent.Comp.MaxDamageThreshold - ent.Comp.MinDamageThreshold)).Float();
        if (ent.Comp.MaxDamageThreshold > damageDealt && !_random.Prob(chance))
            return;

        var bloodQuantity = FixedPoint2.Min(damageDealt * ent.Comp.DamageToVolumeFactor * _random.NextFloat(1 / ent.Comp.EmissionSpreadVolume, ent.Comp.EmissionSpreadVolume), ent.Comp.MaxSplatterVolume * ent.Comp.MaxSplatterCount);
        if (!TryTakeBlood(body, bloodQuantity, out var bloodSolution))
            return;

        _audio.PlayPvs(ent.Comp.SplatterSound, ent);

        var bodyPosition = _transform.GetWorldPosition(body);
        var originPosition = _transform.GetWorldPosition(origin);

        var originalDirection = (bodyPosition - originPosition).Normalized();
        if (!originalDirection.IsValid())
            originalDirection = _random.NextVector2();

        var coordinates = _transform.GetMapCoordinates(body);

        switch (ent.Comp.SplatterType)
        {
            case SplatterType.Random:
            {
                SpawnRandomly(ent, damageDealt, coordinates, bloodSolution, originalDirection);
                break;
            }
            case SplatterType.Line:
            {
                SpawnLinearly(ent, coordinates, bloodSolution, originalDirection);
                break;
            }
        }
    }

    #endregion

    #region Private API

    private Vector2 GetEmissionSpreadDirection(float emissionSpreadAngle, Vector2 direction)
    {
        var randomOffset = _random.NextFloat(-emissionSpreadAngle, emissionSpreadAngle);

        var cosOffset = MathF.Cos(randomOffset);
        var sinOffset = MathF.Sin(randomOffset);

        var newX = direction.X * cosOffset - direction.Y * sinOffset;
        var newY = direction.X * sinOffset + direction.Y * cosOffset;

        return new (newX, newY);
    }

    private void SpawnLinearly(
        Entity<BloodSplatterWoundComponent> ent,
        MapCoordinates coordinates,
        Solution bloodSolution,
        Vector2 direction
        )
    {
        var finalDirection = GetEmissionSpreadDirection((float)ent.Comp.EmissionSpreadAngle.Theta, direction);
        var splatterCount = Math.Ceiling((bloodSolution.Volume / ent.Comp.MaxSplatterVolume).Float());

        for (var i = 1; i < splatterCount + 1; i++)
        {
            var particleSolution = bloodSolution.SplitSolution(bloodSolution.Volume / splatterCount);

            var particle = _particle.SpawnLiquidParticle(ent.Comp.SplatterPrototype, coordinates, finalDirection, i * ent.Comp.EmissionSpreadDistance, particleSolution);
            if (!particle.HasValue)
                continue;

            _transform.SetWorldRotation(particle.Value, finalDirection.ToWorldAngle());
        }
    }

    private void SpawnRandomly(
        Entity<BloodSplatterWoundComponent> ent,
        FixedPoint2 damageDealt,
        MapCoordinates coordinates,
        Solution bloodSolution,
        Vector2 direction
    )
    {
        var splatterCount = Math.Ceiling((bloodSolution.Volume / ent.Comp.MaxSplatterVolume).Float());

        for (var i = 0; i < splatterCount; i++)
        {
            var finalDirection = GetEmissionSpreadDirection((float)ent.Comp.EmissionSpreadAngle.Theta, direction);
            var distance = damageDealt.Float() * ent.Comp.DamageToDistanceFactor * _random.NextFloat(1 / ent.Comp.EmissionSpreadDistance, ent.Comp.EmissionSpreadDistance);
            var particleSolution = bloodSolution.SplitSolution(bloodSolution.Volume / splatterCount);

            _particle.SpawnLiquidParticle(ent.Comp.SplatterPrototype, coordinates, finalDirection, distance, particleSolution);
        }
    }

    #endregion
}
