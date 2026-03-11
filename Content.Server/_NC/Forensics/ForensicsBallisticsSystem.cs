using Content.Shared._NC.Forensics;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Projectiles;
using Content.Shared.Body.Part;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared._Shitmed.Medical.Surgery.Tools;
using Content.Shared.Tag;
using Content.Shared.Chat;
using Content.Server.Projectiles;
using Content.Server.Hands.Systems;
using Content.Server.Chat.Managers;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using System.Linq;
using Content.Shared._Lavaland.Weapons.Ranged.Events;

namespace Content.Server._NC.Forensics;

public sealed class ForensicsBallisticsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private const int MaxStuckBullets = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForensicsProjectileHashComponent, ProjectileHitEvent>(OnProjectileHit);
        
        // Инструменты
        SubscribeLocalEvent<BodyComponent, InteractUsingEvent>(OnInteractUsing);
        
        // DoAfters
        SubscribeLocalEvent<BodyComponent, BallisticIncisionDoAfterEvent>(OnIncisionDone);
        SubscribeLocalEvent<BodyComponent, BallisticExtractionDoAfterEvent>(OnExtractionDone);

        // Хэширование оружия (MapInit)
        SubscribeLocalEvent<ForensicsWeaponHashComponent, MapInitEvent>(OnHashMapInit);
        
        // Перехват выстрела
        SubscribeLocalEvent<GunComponent, ProjectileShotEvent>(OnProjectileShot);
    }

    private void OnHashMapInit(EntityUid uid, ForensicsWeaponHashComponent component, MapInitEvent args)
    {
        if (string.IsNullOrWhiteSpace(component.Hash))
        {
            component.Hash = GenerateRandomHash();
            Dirty(uid, component);
        }
    }

    private void OnProjectileShot(EntityUid uid, GunComponent component, ProjectileShotEvent args)
    {
        var hashComp = EnsureComp<ForensicsWeaponHashComponent>(uid);
        if (string.IsNullOrWhiteSpace(hashComp.Hash))
        {
            hashComp.Hash = GenerateRandomHash();
            Dirty(uid, hashComp);
        }

        var projHash = EnsureComp<ForensicsProjectileHashComponent>(args.FiredProjectile);
        projHash.Hash = hashComp.Hash;
    }

    private void OnProjectileHit(EntityUid uid, ForensicsProjectileHashComponent component, ref ProjectileHitEvent args)
    {
        if (!_random.Prob(0.4f)) 
            return;

        var target = args.Target;
        if (!TryComp<BodyComponent>(target, out var body))
            return;

        int totalBullets = 0;
        foreach (var part in _body.GetBodyChildren(target))
        {
            if (_container.TryGetContainer(part.Id, "forensics_bullets", out var cont))
                totalBullets += cont.ContainedEntities.Count;
        }

        if (totalBullets >= MaxStuckBullets)
            return;

        var hitPart = _body.GetBodyChildren(target).FirstOrDefault(p => p.Component.PartType == BodyPartType.Torso).Id;
        
        if (!hitPart.Valid)
            hitPart = _body.GetBodyChildren(target).FirstOrDefault().Id;

        if (!hitPart.Valid)
            return;

        var stuckBullet = Spawn("StuckBullet", Transform(hitPart).Coordinates);
        var stuckComp = EnsureComp<ForensicsStuckBulletComponent>(stuckBullet);
        stuckComp.Hash = component.Hash;
        
        var container = _container.EnsureContainer<ContainerSlot>(hitPart, "forensics_bullets");
        _container.Insert(stuckBullet, container);

        // Уведомление в чат жертве
        if (_playerManager.TryGetSessionByEntity(target, out var session))
        {
            var msg = "Вы чувствуете, как пуля застряла глубоко в плоти!";
            _chatManager.ChatMessageToOne(ChatChannel.Local, msg, msg, target, false, session.Channel, Robust.Shared.Maths.Color.Red);
        }

        _popup.PopupEntity("Вы чувствуете резкую боль от застрявшей пули", target, target, PopupType.LargeCaution);
    }

    private void OnInteractUsing(EntityUid uid, BodyComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (!TryComp<TargetingComponent>(args.User, out var targeting))
            return;

        var partEntity = GetPartEntityByTarget(uid, targeting.Target);
        if (partEntity == null) return;

        if (!_container.TryGetContainer(partEntity.Value, "forensics_bullets", out var container) || container.ContainedEntities.Count == 0)
            return;

        var bullet = container.ContainedEntities.First();
        if (!TryComp<ForensicsStuckBulletComponent>(bullet, out var stuck))
            return;

        if (HasComp<ScalpelComponent>(args.Used))
        {
            if (stuck.IncisionMade)
            {
                _popup.PopupEntity("Надрез здесь уже сделан", uid, args.User);
                return;
            }

            var doAfter = new DoAfterArgs(EntityManager, args.User, 3f, new BallisticIncisionDoAfterEvent(), uid, target: uid, used: args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };
            _doAfter.TryStartDoAfter(doAfter);
            args.Handled = true;
        }
        else if (HasComp<TweezersComponent>(args.Used) || HasComp<HemostatComponent>(args.Used))
        {
            if (!stuck.IncisionMade)
            {
                _popup.PopupEntity("Сначала нужно сделать надрез скальпелем", uid, args.User);
                return;
            }

            var doAfter = new DoAfterArgs(EntityManager, args.User, 5f, new BallisticExtractionDoAfterEvent(), uid, target: uid, used: args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };
            _doAfter.TryStartDoAfter(doAfter);
            args.Handled = true;
        }
    }

    private void OnIncisionDone(EntityUid uid, BodyComponent component, BallisticIncisionDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null || args.User == null) return;

        if (!TryComp<TargetingComponent>(args.User, out var targeting)) return;
        var partEntity = GetPartEntityByTarget(uid, targeting.Target);
        if (partEntity == null) return;

        if (!_container.TryGetContainer(partEntity.Value, "forensics_bullets", out var container) || container.ContainedEntities.Count == 0)
            return;

        var bullet = container.ContainedEntities.First();
        if (TryComp<ForensicsStuckBulletComponent>(bullet, out var stuck))
        {
            stuck.IncisionMade = true;
            Dirty(bullet, stuck);
            _popup.PopupEntity("Надрез сделан. Теперь пулю можно извлечь", uid, args.User);
        }
    }

    private void OnExtractionDone(EntityUid uid, BodyComponent component, BallisticExtractionDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null || args.User == null) return;

        if (!TryComp<TargetingComponent>(args.User, out var targeting)) return;
        var partEntity = GetPartEntityByTarget(uid, targeting.Target);
        if (partEntity == null) return;

        if (!_container.TryGetContainer(partEntity.Value, "forensics_bullets", out var container) || container.ContainedEntities.Count == 0)
            return;

        var bulletEntity = container.ContainedEntities.First();
        if (TryComp<ForensicsStuckBulletComponent>(bulletEntity, out var stuck))
        {
            var hash = stuck.Hash;
            
            var deformedBullet = Spawn("DeformedBullet", Transform(uid).Coordinates);
            var bulletComp = EnsureComp<ForensicsBulletComponent>(deformedBullet);
            bulletComp.Hash = hash;

            _hands.TryPickupAnyHand(args.User, deformedBullet);

            _popup.PopupEntity("Пуля извлечена", uid, args.User);
            QueueDel(bulletEntity);
        }
    }

    private EntityUid? GetPartEntityByTarget(EntityUid body, TargetBodyPart target)
    {
        var bodyParts = _body.GetBodyChildren(body).ToList();
        var partType = BodyPartType.Torso;
        switch (target)
        {
            case TargetBodyPart.Head: partType = BodyPartType.Head; break;
            case TargetBodyPart.Torso: partType = BodyPartType.Torso; break;
            case TargetBodyPart.LeftArm: partType = BodyPartType.Arm; break; 
            case TargetBodyPart.RightArm: partType = BodyPartType.Arm; break;
            case TargetBodyPart.LeftLeg: partType = BodyPartType.Leg; break;
            case TargetBodyPart.RightLeg: partType = BodyPartType.Leg; break;
        }

        foreach (var part in bodyParts)
        {
            if (part.Component.PartType == partType)
            {
                var partName = Name(part.Id).ToLower();
                if (target.ToString().Contains("Left") && (partName.Contains("левая") || partName.Contains("left")))
                    return part.Id;
                if (target.ToString().Contains("Right") && (partName.Contains("правая") || partName.Contains("right")))
                    return part.Id;
            }
        }
        return bodyParts.FirstOrDefault(p => p.Component.PartType == partType).Id;
    }

    private string GenerateRandomHash()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var part1 = new string(Enumerable.Repeat(chars, 3)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
        var part2 = new string(Enumerable.Repeat(chars, 2)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
        return $"{part1}-{part2}";
    }
}
