using Content.Server.Chat.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Goobstation.Actions;
using Content.Shared._Goobstation.Wizard;
using Content.Shared.Chat;
using Content.Shared.Magic.Components;
using Content.Shared.Speech.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;


namespace Content.Server._Goobstation.Wizard.Systems;


public sealed class SpellsSystem : SharedSpellsSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void CreateChargeEffect(EntityUid uid, ChargeSpellRaysEffectEvent ev)
    {
        RaiseNetworkEvent(ev, Filter.PvsExcept(uid));
    }

    protected override bool ChargeItem(EntityUid uid, ChargeMagicEvent ev)
    {
        if (!TryComp(uid, out BatteryComponent? battery) || battery.CurrentCharge >= battery.MaxCharge)
            return false;

        if (Tag.HasTag(uid, ev.WandTag))
        {
            var difference = battery.MaxCharge - battery.CurrentCharge;
            var charge = MathF.Min(difference, ev.WandChargeRate);
            var degrade = charge * ev.WandDegradePercentagePerCharge;
            var afterDegrade = MathF.Max(ev.MinWandDegradeCharge, battery.MaxCharge - degrade);
            if (battery.MaxCharge > ev.MinWandDegradeCharge)
                _battery.SetMaxCharge(uid, afterDegrade, battery);
            _battery.AddCharge(uid, charge, battery);
        }
        else
            _battery.SetCharge(uid, battery.MaxCharge, battery);

        PopupCharged(uid, ev.Performer, false);
        return true;
    }

    protected override bool Polymorph(PolymorphSpellEvent ev)
    {
        if (ev.ProtoId == null)
            return false;

        var newEnt = _polymorph.PolymorphEntity(ev.Performer, ev.ProtoId.Value);

        if (newEnt == null)
            return false;

        if (ev.MakeWizard)
        {
            if (HasComp<WizardComponent>(ev.Performer))
                EnsureComp<WizardComponent>(newEnt.Value);
            if (HasComp<ApprenticeComponent>(ev.Performer))
                EnsureComp<ApprenticeComponent>(newEnt.Value);
        }

        Audio.PlayPvs(ev.Sound, newEnt.Value);

        var school = MagicSchool.Transmutation;
        if (TryComp(ev.Action.Owner, out MagicComponent? magic))
            school = magic.School;

        if (ev.LoadActions)
            RaiseNetworkEvent(new LoadActionsEvent(GetNetEntity(ev.Performer)), newEnt.Value);

        if (TryComp(ev.Action.Owner, out SpeakOnActionComponent? speak))
        {
            DelayedSpeech(speak.Sentence == null ? null : Loc.GetString(speak.Sentence.Value),
                newEnt.Value,
                ev.Performer,
                school);
        }

        return true;
    }

    private void DelayedSpeech(string? speech, EntityUid speaker, EntityUid caster, MagicSchool school)
    {
        Timer.Spawn(200,
            () =>
            {
                var toSpeak = speech == null ? string.Empty : Loc.GetString(speech);
                SpeakSpell(speaker, caster, toSpeak, school);
            });
    }

    public override void SpeakSpell(EntityUid speakerUid, EntityUid casterUid, string speech, MagicSchool school)
    {
        base.SpeakSpell(speakerUid, casterUid, speech, school);

        if (!Exists(speakerUid))
            return;

        Color? color = null;

        if (Exists(casterUid))
        {
            // var invocationEv = new GetSpellInvocationEvent(school, casterUid);    WD edit - uncomment once Chuuni invocations get ported
            // RaiseLocalEvent(casterUid, invocationEv);
            // if (invocationEv.Invocation != null)
            //     speech = Loc.GetString(invocationEv.Invocation);
            // if (invocationEv.ToHeal.GetTotal() > FixedPoint2.Zero)
            // {
            //     // Heal both caster and speaker
            //     Damageable.TryChangeDamage(casterUid,
            //         -invocationEv.ToHeal,
            //         true,
            //         false,
            //         targetPart: TargetBodyPart.All,
            //         splitDamage: SplitDamageBehavior.SplitEnsureAll);
            //
            //     if (speakerUid != casterUid)
            //     {
            //         Damageable.TryChangeDamage(speakerUid,
            //             -invocationEv.ToHeal,
            //             true,
            //             false,
            //             targetPart: TargetBodyPart.All,
            //             splitDamage: SplitDamageBehavior.SplitEnsureAll);
            //     }
            // }
            //
            // if (speakerUid != casterUid)
            // {
            //     var colorEv = new GetMessageColorOverrideEvent();
            //     RaiseLocalEvent(casterUid, colorEv);
            //     color = colorEv.Color;
            // }
        }

        _chat.TrySendInGameICMessage(speakerUid,
            speech,
            InGameICChatType.Speak,
            false);
    }
}
