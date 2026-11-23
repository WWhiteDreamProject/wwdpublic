using Content.Shared._White.Pain;
using Content.Shared._White.Pain.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs;

namespace Content.Server._White.Pain.Systems;

public sealed partial class PainfulSystem
{
    #region Event Handling

    protected override void OnPainLevelChanged(Entity<PainThresholdsComponent> ent, PainLevel oldLevel)
    {
        if (!ent.Comp.PainEffects.ContainsKey(ent.Comp.Level))
        {
            ent.Comp.NextPainEffects = null;
            return;
        }

        if (ent.Comp.Level > oldLevel)
        {
            ApplyPainEffects(ent);
            return;
        }

        ent.Comp.NextPainEffects ??= GetNextPainEffectsTime(ent);
    }

    #endregion

    #region Private API

    private void UpdatePainEffects()
    {
        var curTime = GameTiming.CurTime;
        var query = EntityQueryEnumerator<PainThresholdsComponent>();
        while (query.MoveNext(out var uid, out var thresholds))
        {
            if (thresholds.NextPainEffects is not { } next || curTime < next)
                continue;

            ApplyPainEffects((uid, thresholds));
        }
    }

    private void ApplyPainEffects(Entity<PainThresholdsComponent> ent)
    {
        if (!ent.Comp.PainEffects.TryGetValue(ent.Comp.Level, out var effects))
        {
            ent.Comp.NextPainEffects = null;
            return;
        }

        ent.Comp.NextPainEffects = GetNextPainEffectsTime(ent);

        if (ent.Comp.MobState == MobState.Dead)
            return;

        var args = new EntityEffectBaseArgs(ent, EntityManager);
        foreach (var effect in effects)
        {
            effect.Effect(args);
        }
    }

    private TimeSpan GetNextPainEffectsTime(Entity<PainThresholdsComponent> ent)
    {
        var variation = ent.Comp.PainEffectsIntervalVariation;
        return GameTiming.CurTime + ent.Comp.PainEffectsInterval * _random.NextFloat(1f - variation, 1f + variation);
    }

    #endregion
}
