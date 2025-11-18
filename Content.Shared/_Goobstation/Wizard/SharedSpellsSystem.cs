using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Carrying;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Magic;
using Content.Shared.Magic.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;


namespace Content.Shared._Goobstation.Wizard;


public abstract class SharedSpellsSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly TagSystem Tag = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private   readonly SharedChargesSystem _charges = default!;
    [Dependency] private   readonly SharedMagicSystem _magic = default!;
    [Dependency] private   readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<PolymorphSpellEvent>(OnPolymorph);
        SubscribeLocalEvent<ChargeMagicEvent>(OnCharge);
    }

    private void OnCharge(ChargeMagicEvent ev) // TODO: Make this work, instead of ChargeSpellEvent
    {
        if (ev.Handled || !_magic.PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        ev.Handled = true;

        var raysEv = new ChargeSpellRaysEffectEvent(GetNetEntity(ev.Performer));
        CreateChargeEffect(ev.Performer, raysEv);

        if (TryComp<PullerComponent>(ev.Performer, out var puller) && HasComp<PullableComponent>(puller.Pulling) &&
            RechargePerson(puller.Pulling.Value))
            return;

        if (TryComp(ev.Performer, out CarryingComponent? carrying) && RechargePerson(carrying.Carried))
            return;

        if (!TryComp(ev.Performer, out HandsComponent? hands))
            return;

        foreach (var item in Hands.EnumerateHeld(ev.Performer, hands))
        {
            if (Tag.HasAnyTag(item, ev.RechargeTags))
            {
                if (TryComp<LimitedChargesComponent>(item, out var limitedCharges))
                {
                    _charges.SetCharges(item, limitedCharges.MaxCharges, limitedCharges.MaxCharges);
                    PopupCharged(item, ev.Performer);
                    break;
                }

                if (TryComp<BasicEntityAmmoProviderComponent>(item, out var basicAmmoComp) &&
                    basicAmmoComp is { Count: not null, Capacity: not null } &&
                    basicAmmoComp.Count < basicAmmoComp.Capacity)
                {
                    _gunSystem.UpdateBasicEntityAmmoCount(item, basicAmmoComp.Capacity.Value, basicAmmoComp);
                    PopupCharged(item, ev.Performer);
                    break;
                }
            }

            if (ChargeItem(item, ev))
                break;
        }

        return;

        bool RechargePerson(EntityUid uid)
        {
            if (RechargeAllSpells(uid))
            {
                PopupCharged(uid, ev.Performer, false);
                _popup.PopupEntity(Loc.GetString("spell-charge-spells-charged-pulled"), uid, uid, PopupType.Medium);
                ev.Handled = true;
                return true;
            }

            _popup.PopupEntity(Loc.GetString("spell-charge-no-spells-to-charge-pulled"), uid, uid, PopupType.Medium);
            return false;
        }
    }

    private void OnPolymorph(PolymorphSpellEvent ev)
    {
        if (ev.Handled || !_magic.PassesSpellPrerequisites(ev.Action, ev.Performer))
            return;

        ev.Handled = Polymorph(ev);
    }

    #region Helpers

    public abstract void CreateChargeEffect(EntityUid uid, ChargeSpellRaysEffectEvent ev);

    protected void PopupCharged(EntityUid uid, EntityUid performer, bool client = true)
    {
        var message = Loc.GetString("spell-charge-spells-charged-entity",
            ("entity", Identity.Entity(uid, EntityManager)));
        if (client)
            PopupLoc(performer, message, PopupType.Medium);
        else
            _popup.PopupEntity(message, performer, performer, PopupType.Medium);
    }

    private bool RechargeAllSpells(EntityUid uid, EntityUid? except = null)
    {
        var magicQuery = GetEntityQuery<MagicComponent>();
        var ents = except != null
            ? Actions.GetActions(uid).Where(x => x.Id != except.Value && magicQuery.HasComp(x.Id))
            : Actions.GetActions(uid).Where(x => magicQuery.HasComp(x.Id));
        var hasSpells = false;
        foreach (var (ent, _) in ents)
        {
            hasSpells = true;
            Actions.SetCooldown(ent, TimeSpan.Zero);
        }

        return hasSpells;
    }



    #endregion

    #region ServerMethods

    protected virtual bool ChargeItem(EntityUid uid, ChargeMagicEvent ev)
    {
        return true;
    }

    protected virtual bool Polymorph(PolymorphSpellEvent ev)
    {
        return true;
    }
    public virtual void SpeakSpell(EntityUid speakerUid, EntityUid casterUid, string speech, MagicSchool school) { }

    #endregion
    #region Helpers
    private void PopupLoc(EntityUid uid, string locMessage, PopupType type = PopupType.Small)
    {
        _popup.PopupClient(locMessage, uid, uid, type);
    }
    #endregion
}

[Serializable, NetSerializable]
public sealed class ChargeSpellRaysEffectEvent(NetEntity uid) : EntityEventArgs
{
    public NetEntity Uid = uid;
}
