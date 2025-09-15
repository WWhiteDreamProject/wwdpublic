using Content.Shared.Mood;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared._White.Psionics;
using Content.Shared.Humanoid;
using Content.Shared.Damage;
using Content.Shared._Shitmed.Targeting;
using Content.Server.Speech.Components;
using Content.Server.Lightning;
using Content.Server.Stunnable;
using Content.Server.Medical;
using Content.Server.Abilities.Psionics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Damage.Prototypes;
using Content.Shared.NPC.Systems;

namespace Content.Server._White.Psionics;

public sealed class PsionicOverloadSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly PsionicAbilitiesSystem _psionics = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;

    private float _updateTimer;
    private const float UpdateInterval = 4f;
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        _updateTimer += frameTime;
        if (_updateTimer < UpdateInterval)
            return;
            
        _updateTimer = 0f;
        
        var query = EntityQueryEnumerator<NoospherePressureComponent>();
        while (query.MoveNext(out var uid, out var pressure))
        {
            if(!TryComp<PsionicComponent>(uid, out var psionic))
                return;
            
            if(!pressure.staticPressure)
                pressure.Pressure = Math.Max(0, pressure.Pressure - pressure.DecayRate);

            if(pressure.Pressure > pressure.MaxPressure)
                ApplyCriticalPunishment(uid, psionic);
            else if(pressure.Pressure > pressure.MediumPressure)
                ApplyRandomPsionicDebuff(uid);
        }
    }
    
    private void ApplyRandomPsionicDebuff(EntityUid uid)
    {
        if(!TryComp<PsionicComponent>(uid, out var psionic))
            return;
        var pow = psionic.CurrentAmplification * 4;
        var random = IoCManager.Resolve<IRobustRandom>();
        var debuffs = new List<Action<EntityUid>>
        {
            target => ApplyTemporaryBlindness(target, pow * 3f),
            target => ApplyAccent(target, pow * 7f),
            target => RandomTeleport(target, pow),
            target => RandomZap(target, 3f + pow, (int)Math.Floor(pow * 0.5f), pow),
            target => SpawnWisp(target),
            target => BecomeHostile(target),
        };
        
        var randomDebuff = debuffs[random.Next(debuffs.Count)];
        randomDebuff(uid);
    }
    
    private void ApplyTemporaryBlindness(EntityUid target, float duration)
    {
        _statusEffects.TryAddStatusEffect<BlurryVisionComponent>(target, "BlurryVision", 
            TimeSpan.FromSeconds(duration), true);
        _popup.PopupEntity(Loc.GetString("psionic-overload-blurry"), target, target);
    }

    private void ApplyAccent(EntityUid target, float duration)
    {
        _statusEffects.TryAddStatusEffect<ScrambledAccentComponent>(target, "Stutter", 
            TimeSpan.FromSeconds(duration), true);
        _popup.PopupEntity(Loc.GetString("psionic-overload-scramble"), target, target);
    }

    private void RandomTeleport(EntityUid target, float distance)
    {
        var coordinates = Transform(target).Coordinates;
        _xform.SetCoordinates(target, coordinates.Offset(_random.NextVector2(distance)));
        _popup.PopupEntity(Loc.GetString("psionic-overload-teleport"), target, target);
    }

    private void RandomZap(EntityUid target, float range, int boltCount, float stunTime)
    {
        _stun.TryParalyze(target, TimeSpan.FromSeconds(stunTime), true);
        _lightning.ShootRandomLightnings(_xform.GetMapCoordinates(target), range, boltCount);
        _popup.PopupEntity(Loc.GetString("psionic-overload-zap"), target, target);
    }

    private void SpawnWisp(EntityUid target) 
    {
        _vomit.Vomit(target);
        var wisp = Spawn("MobGlimmerWispOverload", _xform.GetMapCoordinates(target));
        _popup.PopupEntity(Loc.GetString("psionic-overload-mite"), target, target);
    }

    private void BecomeHostile(EntityUid target) 
    {
        _npcFactionSystem.AddFaction(target, "AllHostile");
        _npcFactionSystem.RemoveFaction(target, "Nanotrasen");
        _popup.PopupEntity(Loc.GetString("psionic-overload-hostile"), target, target);
    }

    private void ApplyCriticalPunishment(EntityUid uid, PsionicComponent psionic) 
    {
        var amplification = psionic.CurrentAmplification;

        if (amplification >= 3.5f)
        {
            ApplyTier4Punishment(uid);
        }
        else if (amplification >= 2.6f)
        {
            ApplyTier3Punishment(uid);
        }
        else if (amplification >= 1.6f)
        {
            ApplyTier2Punishment(uid);
        }
        else
        {
            ApplyTier1Punishment(uid);
        }
    }

    private void ApplyTier1Punishment(EntityUid uid)
    {
        RemComp<PsionicComponent>(uid);
        _popup.PopupEntity(Loc.GetString("psionic-punishment-tier1"), uid, uid);
    }

    private void ApplyTier2Punishment(EntityUid uid)
    {

        RemComp<PsionicComponent>(uid);
    
        if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
        {
            appearance.Species = "Vulpkanin";
            appearance.BodyType = "VulpkaninNormal";
        }
    
        _popup.PopupEntity(Loc.GetString("psionic-punishment-tier2"), uid, uid);
    }

    private void ApplyTier3Punishment(EntityUid uid)
    {
        _psionics.MindBreak(uid);
        _popup.PopupEntity(Loc.GetString("psionic-punishment-tier3"), uid, uid);
    }

    private void ApplyTier4Punishment(EntityUid uid)
    {
        _psionics.MindBreak(uid);
        var damageSpecifier = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Brute"), 999);
        _damageable.TryChangeDamage(uid, damageSpecifier, true, targetPart: TargetBodyPart.Head);
        _popup.PopupEntity(Loc.GetString("psionic-punishment-tier4"), uid, uid);
    }
}