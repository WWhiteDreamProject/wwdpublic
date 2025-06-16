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
    
    // For studio visor
    private bool _hasStudioVisor = false;
    private const float StudioVisorGlitchReduction = 0.7f; // Reduce glitch intensity by 70% with studio visor

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CRTVisionComponent, ComponentInit>(OnCRTVisionInit);
        SubscribeLocalEvent<CRTVisionComponent, ComponentShutdown>(OnCRTVisionShutdown);
        SubscribeLocalEvent<CRTVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CRTVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        // Subscribe to studio visor events
        SubscribeLocalEvent<StudioVisorComponent, ComponentInit>(OnStudioVisorInit);
        SubscribeLocalEvent<StudioVisorComponent, ComponentShutdown>(OnStudioVisorShutdown);
        SubscribeLocalEvent<StudioVisorComponent, LocalPlayerAttachedEvent>(OnStudioVisorAttached);
        SubscribeLocalEvent<StudioVisorComponent, LocalPlayerDetachedEvent>(OnStudioVisorDetached);

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

        // Check if player has studio visor
        _hasStudioVisor = _entityManager.HasComponent<StudioVisorComponent>(uid);

        // Only add full CRT overlay if player doesn't have studio visor
        if (!_hasStudioVisor && !_cfg.GetCVar(CCVars.NoVisionFilters) && !_overlayMan.HasOverlay<CRTVisionOverlay>())
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

        _hasStudioVisor = false;
    }

    private void OnPlayerAttached(EntityUid uid, CRTVisionComponent component, LocalPlayerAttachedEvent args)
    {
        // Check if player has studio visor
        _hasStudioVisor = _entityManager.HasComponent<StudioVisorComponent>(uid);

        // Only add full CRT overlay if player doesn't have studio visor
        if (!_hasStudioVisor && !_cfg.GetCVar(CCVars.NoVisionFilters) && !_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.AddOverlay(_overlay);

        // Initialize health percentage
        UpdateHealthPercentage(uid);
    }

    private void OnPlayerDetached(EntityUid uid, CRTVisionComponent component, LocalPlayerDetachedEvent args)
    {
        if (_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
        
        _hasStudioVisor = false;
    }

    // Studio Visor event handlers
    private void OnStudioVisorInit(EntityUid uid, StudioVisorComponent component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        _hasStudioVisor = true;

        // Remove CRT overlay if it exists, as studio visor disables the base effect
        if (_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnStudioVisorShutdown(EntityUid uid, StudioVisorComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        _hasStudioVisor = false;

        // Re-add CRT overlay if player still has CRT vision component
        if (_entityManager.HasComponent<CRTVisionComponent>(uid) && 
            !_cfg.GetCVar(CCVars.NoVisionFilters) && 
            !_overlayMan.HasOverlay<CRTVisionOverlay>())
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnStudioVisorAttached(EntityUid uid, StudioVisorComponent component, LocalPlayerAttachedEvent args)
    {
        _hasStudioVisor = true;

        // Remove CRT overlay if it exists, as studio visor disables the base effect
        if (_overlayMan.HasOverlay<CRTVisionOverlay>())
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnStudioVisorDetached(EntityUid uid, StudioVisorComponent component, LocalPlayerDetachedEvent args)
    {
        _hasStudioVisor = false;

        // Re-add CRT overlay if player still has CRT vision component
        if (_entityManager.HasComponent<CRTVisionComponent>(uid) && 
            !_cfg.GetCVar(CCVars.NoVisionFilters) && 
            !_overlayMan.HasOverlay<CRTVisionOverlay>())
        {
            _overlayMan.AddOverlay(_overlay);
        }
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
            // Only add overlay if player doesn't have studio visor
            if (!_hasStudioVisor && 
                !_overlayMan.HasOverlay<CRTVisionOverlay>() && 
                _playerMan.LocalEntity is { Valid: true } player &&
                EntityManager.HasComponent<CRTVisionComponent>(player))
            {
                _overlayMan.AddOverlay(_overlay);
            }
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
        
        // If player has studio visor, we still want to show glitches at low health
        // but only if health is very low or on impact
        if (_hasStudioVisor && _healthPercentage < 0.3f)
        {
            // Add temporary overlay for low health with studio visor
            if (!_overlayMan.HasOverlay<CRTVisionOverlay>())
                _overlayMan.AddOverlay(_overlay);
        }
        else if (_hasStudioVisor && _healthPercentage >= 0.3f && _impactDarkness <= 0.01f)
        {
            // Remove overlay when health is restored and no impact effects
            if (_overlayMan.HasOverlay<CRTVisionOverlay>())
                _overlayMan.RemoveOverlay(_overlay);
        }
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
        
        // Reduce impact effect intensity if player has studio visor
        float effectiveIntensity = _hasStudioVisor ? intensity * (1.0f - StudioVisorGlitchReduction) : intensity;
        _impactDarkness = Math.Min(MaxImpactDarkness, effectiveIntensity / 30.0f);

        // Apply temporary glitch effect on damage
        float glitchIntensity = Math.Min(0.7f, effectiveIntensity / 20.0f);
        _overlay.SetTemporaryGlitchEffect(glitchIntensity, 0.25f);

        // Update shader parameter
        _overlay.SetImpactDarkness(_impactDarkness);
        
        // If player has studio visor, temporarily add overlay for impact effect
        if (_hasStudioVisor && !_overlayMan.HasOverlay<CRTVisionOverlay>())
        {
            _overlayMan.AddOverlay(_overlay);
        }
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
                
                // If player has studio visor and health is good, remove overlay when impact effect ends
                if (_hasStudioVisor && _healthPercentage >= 0.3f && _overlayMan.HasOverlay<CRTVisionOverlay>())
                {
                    _overlayMan.RemoveOverlay(_overlay);
                }
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
 