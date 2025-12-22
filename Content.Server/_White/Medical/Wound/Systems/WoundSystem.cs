using Content.Server._White.Body.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Medical;
using Content.Server.Medical.Components;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared._White.Medical.Wounds.Components.Woundable;
using Content.Shared._White.Medical.Wounds.Systems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical;
using Content.Shared.Stacks;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server._White.Medical.Wound.Systems;

public sealed class WoundSystem : SharedWoundSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stacks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableComponent, HealingDoAfterEvent>(OnHealingDoAfter, before: new [] { typeof(HealingSystem), });
    }

    #region Event Handling

    private void OnHealingDoAfter(Entity<WoundableComponent> woundable, ref HealingDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || !TryComp<HealingComponent>(args.Used, out var healing)
            || !TryComp<DamageableComponent>(woundable, out var damageableBody)
            || _body.GetBodyParts<DamageableComponent>(woundable.Owner, args.BodyPartType).FirstOrNull() is not {} bodyPart)
            return;

        args.Handled = true;

        if (TryComp<BloodstreamComponent>(woundable, out var bloodstream))
        {
            if (healing.BloodlossModifier != 0)
            {
                var isBleeding = bloodstream.BleedAmount > 0;
                _bloodstream.TryModifyBleedAmount(woundable.Owner, healing.BloodlossModifier);

                if (isBleeding != bloodstream.BleedAmount > 0)
                {
                    var popup = args.User == woundable.Owner
                        ? Loc.GetString("medical-item-stop-bleeding-self")
                        : Loc.GetString(
                            "medical-item-stop-bleeding",
                            ("target", Identity.Entity(woundable.Owner, EntityManager)));

                    _popup.PopupEntity(popup, woundable, args.User);
                }
            }

            if (healing.ModifyBloodLevel != 0)
                _bloodstream.TryModifyBloodLevel(woundable, healing.ModifyBloodLevel, bloodstream);
        }

        if (healing.DamageContainers is not null &&
            bodyPart.Comp2.DamageContainerID is not null &&
            !healing.DamageContainers.Contains(bodyPart.Comp2.DamageContainerID))
            return;

        var healed = Damageable.TryChangeDamage(woundable, healing.Damage, true, false, damageableBody, args.Args.User, args.BodyPartType);

        if (healed == null && healing.BloodlossModifier != 0)
            return;

        var total = healed?.GetTotal() ?? FixedPoint2.Zero;
        var repeat = false;

        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.Use(args.Used.Value, 1, stackComp);
            repeat = _stacks.GetCount(args.Used.Value, stackComp) > 0;
        }
        else
            QueueDel(args.Used.Value);

        if (woundable.Owner != args.User)
            _adminLogger.Add(LogType.Healed, $"{EntityManager.ToPrettyString(args.User):user} healed {EntityManager.ToPrettyString(woundable):target} for {total:damage} damage");
        else
            _adminLogger.Add(LogType.Healed, $"{EntityManager.ToPrettyString(args.User):user} healed themselves for {total:damage} damage");

        _audio.PlayPvs(healing.HealingEndSound, woundable, AudioParams.Default.WithVariation(0.125f).WithVolume(1f));

        args.Repeat = false;

        if (repeat)
        {
            if (HasWounds(bodyPart, healing.Damage))
                args.Repeat = true;
            else if (GetWoundableBodyParts(woundable.AsNullable(), healing.Damage).FirstOrNull() is { } newBodyPart)
            {
                _popup.PopupEntity(Loc.GetString("medical-item-cant-use-switch-body-part", ("item", args.Used), ("body-part", args.BodyPartType), ("new-body-part", newBodyPart.Comp1.Type)), args.Used.Value, args.User);
                args.BodyPartType = newBodyPart.Comp1.Type;
                args.Repeat = true;
            }
        }

        if (!args.Repeat)
        {
            _popup.PopupEntity(
                Loc.GetString("medical-item-finished-using", ("item", args.Used)),
                woundable,
                args.User);
        }
    }

    #endregion
}
