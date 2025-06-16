using Content.Shared.Traits.Assorted.Components;
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

    // For impact darkness effect
    private float _impactDarkness = 0.0f;
    private TimeSpan _lastImpactTime = TimeSpan.Zero;
    private const float ImpactDuration = 0.3f; // Duration of darkness effect in seconds
    private const float MaxImpactDarkness = 0.8f; // Maximum darkness intensity

    // For health tracking
    private float _healthPercentage = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CRTVisionComponent, ComponentInit>(OnCRTVisionInit);
        SubscribeLocalEvent<CRTVisionComponent, ComponentShutdown>(OnCRTVisionShutdown);
        SubscribeLocalEvent<CRTVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CRTVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

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

        _overlay = new();
    }

    private void OnCRTVisionInit(EntityUid uid, CRTVisionComponent component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        if (!_cfg.GetCVar(CCVars.NoVisionFilters) && !_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.AddOverlay(_overlay);

        // Initialize health percentage
        UpdateHealthPercentage(uid);
    }

    private void OnCRTVisionShutdown(EntityUid uid, CRTVisionComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        if (_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, CRTVisionComponent component, LocalPlayerAttachedEvent args)
    {
        if (!_cfg.GetCVar(CCVars.NoVisionFilters) && !_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.AddOverlay(_overlay);

        // Initialize health percentage
        UpdateHealthPercentage(uid);
    }

    private void OnPlayerDetached(EntityUid uid, CRTVisionComponent component, LocalPlayerDetachedEvent args)
    {
        if (_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnNoVisionFiltersChanged(bool enabled)
    {
        if (enabled)
        {
            if (_overlayMan.HasOverlay<CRTVisionOverlay>())
                _overlayMan.RemoveOverlay(_overlay);
        }
        else
        {
            if (!_overlayMan.HasOverlay<CRTVisionOverlay>() && _playerMan.LocalEntity is { Valid: true } player &&
                EntityManager.HasComponent<CRTVisionComponent>(player))
                _overlayMan.AddOverlay(_overlay);
        }
    }

    // Process damage event
    private void OnDamageChanged(EntityUid uid, CRTVisionComponent component, DamageChangedEvent args)
    {
        if (uid != _playerMan.LocalEntity || args.DamageDelta == null)
            return;

        // Check if damage was applied (not healing)
        // Use a simple value for effect intensity
        float damageAmount = 10.0f; // Fixed value for any damage

        // Check if it was damage and not healing
        if (args.DamageIncreased)
        {
            TriggerImpactEffect(damageAmount);
        }

        // Update health percentage for glitch effects
        UpdateHealthPercentage(uid);
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

        // Update health percentage for glitch effects
        UpdateHealthPercentage(uid);
    }

    // Handle health threshold check
    private void OnThresholdChecked(EntityUid uid, CRTVisionComponent component, MobThresholdChecked args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // Trigger impact effect on health threshold check
        TriggerImpactEffect(15.0f);

        // Update health percentage for glitch effects
        UpdateHealthPercentage(uid);
    }

    // Handle attack on player
    private void OnAttacked(EntityUid uid, CRTVisionComponent component, AttackedEvent args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // Trigger impact effect on attack
        TriggerImpactEffect(12.0f);
    }

    // Handle stun event
    private void OnStunned(EntityUid uid, CRTVisionComponent component, StunnedEvent args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        // Trigger impact effect on stun
        TriggerImpactEffect(18.0f);
    }

    // Method to activate darkness effect
    private void TriggerImpactEffect(float intensity)
    {
        // Set last impact time and darkness intensity
        _lastImpactTime = _gameTiming.CurTime;
        _impactDarkness = Math.Min(MaxImpactDarkness, intensity / 30.0f);

        // Apply temporary glitch effect on damage
        _overlay.SetTemporaryGlitchEffect(Math.Min(0.7f, intensity / 20.0f), 0.25f);

        // Update shader parameter
        _overlay.SetImpactDarkness(_impactDarkness);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // If impact darkness effect is active, gradually decrease it
        if (_impactDarkness > 0.0f)
        {
            var timeSinceImpact = (_gameTiming.CurTime - _lastImpactTime).TotalSeconds;

            if (timeSinceImpact >= ImpactDuration)
            {
                // Effect time expired, reset darkness
                _impactDarkness = 0.0f;
            }
            else
            {
                // Linearly decrease darkness over time
                _impactDarkness = MaxImpactDarkness * (1.0f - (float)(timeSinceImpact / ImpactDuration));
            }

            // Update shader parameter
            _overlay.SetImpactDarkness(_impactDarkness);
        }
    }
}
