using System.Numerics;
using Content.Shared._White.Particles.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Particles.Systems;

public sealed partial class ParticleSystem
{
    private void InitializeLiquid() =>
        SubscribeLocalEvent<LiquidParticleComponent, AfterParticleLandedEvent>(OnLiquidAfterParticleLanded);

    private void OnLiquidAfterParticleLanded(Entity<LiquidParticleComponent> ent, ref AfterParticleLandedEvent args)
    {
        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.InitialSolutionName, out _, out var initialSolution))
        {
            Log.Error($"Liquid particle {ToPrettyString(ent)} without {ent.Comp.InitialSolutionName} solution");
            return;
        }

        if (!_solutionContainer.TryGetSolution(args.LandedParticle, ent.Comp.LandSolutionName, out var lanSolution))
        {
            Log.Error($"Liquid landed particle {ToPrettyString(ent)} without {ent.Comp.LandSolutionName} solution");
            return;
        }

        _solutionContainer.TryAddSolution(lanSolution.Value, initialSolution);
    }

    public Entity<ParticleComponent, LiquidParticleComponent>? SpawnLiquidParticle(
        EntProtoId prototype,
        MapCoordinates coordinates,
        Vector2 velocity,
        float distance,
        Solution solution
        )
    {
        if (SpawnParticle(prototype, coordinates, velocity, distance) is not {} particle)
            return null;

        if (!_liquidParticleQuery.TryComp(particle, out var liquidParticleComponent))
        {
            Log.Error($"Fail spawn liquid particle without {nameof(LiquidParticleComponent)}, prototype: {prototype}");
            QueueDel(particle);
            return null;
        }

        if (!_solutionContainer.TryGetSolution(particle.Owner, liquidParticleComponent.InitialSolutionName, out var initialSolution))
        {
            Log.Error($"Liquid particle {ToPrettyString(particle)} without {liquidParticleComponent.InitialSolutionName} solution");
            return null;
        }

        _solutionContainer.ForceAddSolution(initialSolution.Value, solution);

        return (particle, particle, liquidParticleComponent);
    }
}
