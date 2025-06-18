using Content.Shared._White.Traits.Assorted.Components;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using System;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Stunnable;
using Content.Shared.Mobs.Systems;

namespace Content.Client.Overlays;

public sealed partial class CRTVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private CRTVisionOverlay _overlay = default!;

    // For health tracking
    private float _healthPercentage = 1.0f;

    // For studio visor
    private const float StudioVisorGlitchReduction = 0.7f; // Reduce glitch intensity by 70% with studio visor
    private const float StudioVisorLowHealthThreshold = 0.3f;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<CRTVisionComponent, ComponentInit>(OnComponentChange);
        SubscribeLocalEvent<CRTVisionComponent, ComponentShutdown>(OnComponentChange);
        SubscribeLocalEvent<CRTVisionComponent, LocalPlayerAttachedEvent>(OnPlayerStateChange);
        SubscribeLocalEvent<CRTVisionComponent, LocalPlayerDetachedEvent>(OnPlayerStateChange);

        // Subscribe to studio visor events
        SubscribeLocalEvent<StudioVisorComponent, ComponentInit>(OnComponentChange);
        SubscribeLocalEvent<StudioVisorComponent, ComponentShutdown>(OnComponentChange);

        // Subscribe to damage events
        SubscribeLocalEvent<CRTVisionComponent, DamageChangedEvent>(OnDamageChanged);

        // Subscribe to mob state change events
        SubscribeLocalEvent<CRTVisionComponent, MobStateChangedEvent>(OnMobStateChanged);

        // Subscribe to health threshold events
        SubscribeLocalEvent<CRTVisionComponent, MobThresholdChecked>(OnThresholdChecked);

        // Subscribe to attack and stun events
        SubscribeLocalEvent<CRTVisionComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<CRTVisionComponent, StunnedEvent>(OnStunned);

        Subs.CVar(_cfg, CCVars.NoVisionFilters, OnNoVisionFiltersChanged);
    }

    private void OnComponentChange<T>(EntityUid uid, T component, EntityEventArgs args) where T: IComponent
    {
        if (uid == _playerMan.LocalEntity)
            UpdateOverlayState();
    }

    private void OnPlayerStateChange<T>(EntityUid uid, CRTVisionComponent component, T args)
    {
        UpdateOverlayState();
    }

    private void UpdateOverlayState()
    {
        var player = _playerMan.LocalEntity;
        if (player == null || !EntityManager.HasComponent<CRTVisionComponent>(player))
    {
            _overlayMan.RemoveOverlay(_overlay);
            return;
        }

        UpdateHealthPercentage(player.Value);

        var hasStudioVisor = _entityManager.HasComponent<StudioVisorComponent>(player.Value);
        var noVisionFilters = _cfg.GetCVar(CCVars.NoVisionFilters);

        // The overlay is active if filters are enabled, and the user either doesn't have a studio visor,
        // or has one but is at low health.
        bool shouldShowOverlay = !noVisionFilters && (!hasStudioVisor || _healthPercentage < StudioVisorLowHealthThreshold);

        if (shouldShowOverlay)
        {
            if (!_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.AddOverlay(_overlay);
        }
        else
    {
        if (_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnNoVisionFiltersChanged(bool enabled)
    {
        UpdateOverlayState();
    }

    // Process damage event
    private void OnDamageChanged(EntityUid uid, CRTVisionComponent component, DamageChangedEvent args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // Check if it was damage and not healing
        if (args.DamageIncreased && args.DamageDelta != null)
        {
            var damageAmount = (float) args.DamageDelta.GetTotal();
            TriggerImpactEffect(damageAmount);
        }

        // Update health percentage for glitch effects
        UpdateOverlayState();
    }

    // Update health percentage for glitch effects
    private void UpdateHealthPercentage(EntityUid uid)
    {
        if (!_entityManager.TryGetComponent<DamageableComponent>(uid, out var damageable) ||
            !_entityManager.TryGetComponent<MobThresholdsComponent>(uid, out var thresholds))
            return;

        // Get critical threshold
        var mobThresholdSystem = _entityManager.System<MobThresholdSystem>();
        if (!mobThresholdSystem.TryGetIncapThreshold(uid, out var threshold))
            return;

        // Calculate health percentage (1.0 = full health, 0.0 = critical state)
        _healthPercentage = 1.0f - Math.Min(1.0f, (damageable.TotalDamage / threshold.Value).Float());

        // Pass health percentage to shader
        _overlay.SetHealthPercentage(_healthPercentage);
    }

    // Handle mob state change (e.g., transition from Normal to Critical)
    private void OnMobStateChanged(EntityUid uid, CRTVisionComponent component, MobStateChangedEvent args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // If state worsened, show impact effect
        if (args.NewMobState > args.OldMobState)
        {
            TriggerImpactEffect(20.0f); // Stronger effect on state change
        }

        // Trigger a strong glitch effect when entering critical state
        if (args.NewMobState == MobState.Critical)
        {
            _overlay.SetTemporaryGlitchEffect(1.5f, 0.5f);
        }

        // Update health percentage for glitch effects
        UpdateOverlayState();
    }

    // Handle health threshold check
    private void OnThresholdChecked(EntityUid uid, CRTVisionComponent component, MobThresholdChecked args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // Trigger impact effect on health threshold check
        TriggerImpactEffect(15.0f);

        // When crossing a new threshold, trigger a medium glitch effect
        _overlay.SetTemporaryGlitchEffect(0.5f, 0.3f);

        // Update health percentage for glitch effects
        UpdateOverlayState();
    }

    // Handle attack on player
    private void OnAttacked(EntityUid uid, CRTVisionComponent component, AttackedEvent args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // Trigger a small glitch effect on every attack
        _overlay.SetTemporaryGlitchEffect(0.2f, 0.2f);
    }

    // Handle stun event
    private void OnStunned(EntityUid uid, CRTVisionComponent component, StunnedEvent args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // Trigger a medium glitch effect on stun
        _overlay.SetTemporaryGlitchEffect(0.7f, 0.4f);
    }

    private void TriggerImpactEffect(float intensity)
    {
        var player = _playerMan.LocalEntity;
        if (player == null)
            return;

        // Reduce impact effect intensity if player has studio visor
        var effectiveIntensity = intensity;
        if (_entityManager.HasComponent<StudioVisorComponent>(player.Value))
            effectiveIntensity *= (1.0f - StudioVisorGlitchReduction);

        // Trigger a temporary glitch effect proportional to damage
        float glitchIntensity = Math.Min(effectiveIntensity / 50.0f, 1.0f);
        _overlay.SetTemporaryGlitchEffect(glitchIntensity * 0.8f, 0.4f);
    }
}
