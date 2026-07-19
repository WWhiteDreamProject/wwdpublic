using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared._White.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactDamageTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactDamageTriggerComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, ArtifactDamageTriggerComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        foreach (var (type, amount) in args.Damage) // WD EDIT
        {
            if (component.DamageTypes != null && !component.DamageTypes.Contains(type))
                continue;

            component.AccumulatedDamage += (float) amount;
        }

        if (component.AccumulatedDamage >= component.DamageThreshold)
            _artifact.TryActivateArtifact(uid, args.Origin);
    }
}
