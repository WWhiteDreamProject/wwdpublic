using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Damage.Components;
using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Medical.Healing.Components;
using Content.Shared._White.TargetDoll;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Medical.Healing.Systems;

public sealed class HealingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedTargetDollSystem _targetDoll = default!;

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<HealingComponent> _healingQuery;
    private EntityQuery<StackComponent> _stackQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, HealingDoAfterEvent>(OnHealingDoAfter);

        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnUseInHand);

        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _healingQuery = GetEntityQuery<HealingComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
    }

    #region Event Handling

    private void OnHealingDoAfter(Entity<DamageableComponent> ent, ref HealingDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (!_healingQuery.TryComp(args.Used, out var healingComp))
            return;

        var healing = new Entity<HealingComponent>(args.Used.Value, healingComp);

        if (healing.Comp.DamageContainers is not null
            && ent.Comp.DamageContainer is not null
            && !healing.Comp.DamageContainers.Contains(ent.Comp.DamageContainer.Value))
            return;

        if (!_damageable.TryChangeDamage(ent.AsNullable(), healing.Comp.Damage, out var healed, true, false, args.User))
            return;

        var repeat = false;
        if (_stackQuery.TryComp(healing, out var stackComp))
        {
            _stacks.Use(healing, 1, stackComp);

            if (_stacks.GetCount(healing, stackComp) > 0)
                repeat = true;
        }
        else
        {
            PredictedQueueDel(healing.Owner);
        }

        var total = healed.GetTotal();

        if (ent.Owner != args.User)
        {
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} healed {ToPrettyString(ent):target} for {total:damage} damage");
        }
        else
        {
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} healed themselves for {total:damage} damage");
        }

        _audio.PlayPredicted(healing.Comp.HealingEndSound, ent, args.User);

        args.Repeat = _damageable.HasDamage(ent.AsNullable(), healing.Comp.Damage) && repeat;

        if (!args.Repeat)
        {
            _popup.PopupClient(Loc.GetString("medical-item-finished-using", ("item", args.Used)), ent, args.User);
            return;
        }

        if (ent.Owner == args.User)
            args.Args.Delay = healing.Comp.Delay * GetScaledHealingPenalty(ent, healing.Comp.SelfHealPenaltyMultiplier);
    }

    private void OnAfterInteract(Entity<HealingComponent> healing, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryHeal(args.Target.Value, healing, args.User))
            args.Handled = true;
    }

    private void OnUseInHand(Entity<HealingComponent> healing, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryHeal(args.User, healing, args.User))
            args.Handled = true;
    }

    #endregion

    #region Private API

    private bool TryHeal(Entity<DamageableComponent?> ent, Entity<HealingComponent> healing, EntityUid user)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Owner != user && !_interaction.InRangeUnobstructed(ent.Owner, user, popup: true))
            return false;

        if (_stackQuery.TryComp(healing, out var stackComp) && stackComp.Count < 1)
            return false;

        if (healing.Comp.DamageContainers is not null
            && ent.Comp.DamageContainer is not null
            && !healing.Comp.DamageContainers.Contains(ent.Comp.DamageContainer.Value))
            return false;

        var getHealingTargetEv = new GetHealingTargetEvent(_targetDoll.GetSelectedProvider(user), healing);
        RaiseLocalEvent(ent, getHealingTargetEv);

        _popup.PopupClient(getHealingTargetEv.Popup, healing, user);

        if (getHealingTargetEv is { Handled: true, Target: null })
            return false;

        var target = getHealingTargetEv.Target ?? (ent, ent.Comp);

        if (!_damageable.HasDamage(target.AsNullable(), healing.Comp.Damage))
        {
            _popup.PopupClient(Loc.GetString("medical-item-cant-use", ("item", healing.Owner)), healing, user);
            return false;
        }

        _audio.PlayPredicted(healing.Comp.HealingBeginSound, healing, user);

        if (ent.Owner != user)
            _popup.PopupEntity(Loc.GetString("medical-item-popup-target", ("user", Identity.Entity(user, EntityManager)), ("item", healing.Owner)), ent, ent, PopupType.Medium);

        var delay = healing.Comp.Delay;

        if (ent.Owner == user)
            delay *= GetScaledHealingPenalty((ent, ent.Comp), healing.Comp.SelfHealPenaltyMultiplier);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new HealingDoAfterEvent(),
            target,
            ent,
            healing)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(doAfterArgs);
        return true;
    }

    /// <summary>
    /// Scales the self-heal penalty based on the amount of damage taken
    /// </summary>
    /// <param name="ent">Entity we're healing</param>
    /// <param name="mod">Maximum modifier we can have.</param>
    /// <returns>Modifier we multiply our healing time by</returns>
    private float GetScaledHealingPenalty(Entity<DamageableComponent> ent, float mod)
    {
        if (!_mobThreshold.TryGetThresholdForState(ent, MobState.Critical, out var amount))
            return 1;

        var percentDamage = (float)(ent.Comp.TotalDamage / amount);
        //make it scale from 1 to the multiplier.

        var output = percentDamage * (mod - 1) + 1;
        return Math.Max(output, 1);
    }

    #endregion
}

/// <summary>
/// Raised on an entity to get a possible healing target.
/// </summary>
public sealed class GetHealingTargetEvent(BodyProviderType type, Entity<HealingComponent> healing) : HandledEntityEventArgs, IBodyRelayEvent
{
    /// <summary>
    /// Contains the body provider being treated.
    /// </summary>
    public BodyProviderType Type { get; } = type;

    /// <summary>
    /// Contains the healing target if any was responsible.
    /// </summary>
    public Entity<DamageableComponent>? Target;

    /// <summary>
    /// Contains the message to be displayed, if any.
    /// </summary>
    public string? Popup;

    /// <summary>
    /// This is the entity that promotes healing.
    /// </summary>
    public readonly Entity<HealingComponent> Healing = healing;
}

[Serializable, NetSerializable]
public sealed partial class HealingDoAfterEvent : SimpleDoAfterEvent;
