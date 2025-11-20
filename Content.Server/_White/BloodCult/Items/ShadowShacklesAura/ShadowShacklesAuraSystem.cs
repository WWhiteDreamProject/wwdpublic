using Content.Server._White.BloodCult.Items.BaseAura;
using Content.Shared._White.BloodCult.Items.ShadowShacklesAura;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Robust.Shared.Containers;

namespace Content.Server._White.BloodCult.Items.ShadowShacklesAura;

public sealed class ShadowShacklesAuraSystem : BaseAuraSystem<ShadowShacklesAuraComponent>
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowShacklesAuraComponent, EntRemovedFromContainerMessage>(OnShackles);
    }

    private void OnShackles(EntityUid uid, ShadowShacklesAuraComponent component, EntRemovedFromContainerMessage args)
    {
        QueueDel(uid);
        _statusEffects.TryAddStatusEffect<MutedComponent>(component.Target, "Muted", component.MuteDuration, true);
    }
}
