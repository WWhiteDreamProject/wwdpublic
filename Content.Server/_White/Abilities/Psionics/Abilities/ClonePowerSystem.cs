using Content.Shared._White.Abilities.Psionics;
using Content.Shared._White.Actions.Events;
using Content.Shared.Mind;
using Content.Shared.Actions.Events;
using Content.Shared.Abilities.Psionics;
using Content.Server.Humanoid;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;

namespace Content.Server._White.Abilities.Psionics
{
    public sealed class ClonePowerSystem : EntitySystem
    {
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _app = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly MobStateSystem _state = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ClonePowerComponent, ClonePowerActionEvent>(OnPowerUsed);
            SubscribeLocalEvent<ClonePowerComponent, CloneSwitchPowerActionEvent>(CloneSwitch);
            SubscribeLocalEvent<PsionicCloneComponent, OriginalSwitchPowerActionEvent>(OriginalSwitch);
        }

        public void OnPowerUsed(EntityUid uid, ClonePowerComponent component, ClonePowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "clone", true))
                return;

            if (component.CloneUid != null)
                return;

            var humanoid = MetaData(uid).EntityPrototype?.ID;
            var clone = Spawn(humanoid, Transform(uid).Coordinates);
            Transform(clone).AttachToGridOrMap();
            component.CloneUid = clone;

            if (TryComp<MetaDataComponent>(uid, out var name))
                _metaData.SetEntityName(clone, name.EntityName);

            var cloneComp = AddComp<PsionicCloneComponent>(clone);
            cloneComp.OriginalUid = uid;
            _actions.AddAction(clone, "ActionOriginalSwitch");

            RemComp<MindComponent>(clone);

            _app.CloneAppearance(uid, clone);
            _psionics.LogPowerUsed(uid, "clone");
            args.Handled = true;
        }
        public void CloneSwitch(EntityUid uid, ClonePowerComponent component, CloneSwitchPowerActionEvent args)
        {
            if (component.CloneUid == null) {
                _popup.PopupEntity(Loc.GetString("clone-no-clone"), uid, uid);
                return;
            }
            if (!TryComp<MobStateComponent>(component.CloneUid, out var mobState) || _state.IsDead(component.CloneUid.Value, mobState) || _state.IsCritical(component.CloneUid.Value, mobState)) {
                _popup.PopupEntity(Loc.GetString("clone-crit-clone"), uid, uid);
                return;
            }
            if (!_mind.TryGetMind(uid, out var mindId, out var mind))
                return;
            
            _mind.TransferTo(mindId, component.CloneUid, mind: mind);
            args.Handled = true;
        }
        public void OriginalSwitch(EntityUid uid, PsionicCloneComponent component, OriginalSwitchPowerActionEvent args)
        {
            if (component.OriginalUid == null) {
                _popup.PopupEntity(Loc.GetString("clone-no-original"), uid, uid);
                return;
            }
            if (!TryComp<MobStateComponent>(component.OriginalUid, out var mobState) || _state.IsDead(component.OriginalUid.Value, mobState) || _state.IsCritical(component.OriginalUid.Value, mobState)) {
                _popup.PopupEntity(Loc.GetString("clone-crit-original"), uid, uid);
                return;
            }
            if (!_mind.TryGetMind(uid, out var mindId, out var mind))
                return; 
            
            _mind.TransferTo(mindId, component.OriginalUid, mind: mind);
            args.Handled = true;
        }
    }
}
