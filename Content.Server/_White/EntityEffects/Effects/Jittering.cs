using Content.Server.Jittering;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._White.EntityEffects.Effects;

[UsedImplicitly]
public sealed partial class Jittering : EntityEffect
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(1);

    [DataField]
    public bool Refresh = true;

    [DataField]
    public float Amplitude = 10f;

    [DataField]
    public float Frequency = 4f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var jitteringSystem = args.EntityManager.System<JitteringSystem>();

        jitteringSystem.DoJitter(args.TargetEntity, Duration, Refresh, Amplitude, Frequency);
    }
}
