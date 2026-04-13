using Content.Shared._NC.Cyberware.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Overlays.Switchable;
using Content.Shared.Actions;
using Robust.Shared.Containers;

namespace Content.Server._NC.Cyberware.Systems;

public sealed class ServerOpticsCyberwareSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareAntiGlareComponent, EntParentChangedMessage>(OnAntiGlareParentChanged);
        SubscribeLocalEvent<CyberwareIRVisionComponent, EntParentChangedMessage>(OnIRVisionParentChanged);
    }

    private void OnAntiGlareParentChanged(EntityUid uid, CyberwareAntiGlareComponent component, ref EntParentChangedMessage args)
    {
        if (args.OldParent != null && args.OldParent.Value.IsValid())
        {
            RemComp<EyeProtectionComponent>(args.OldParent.Value);
            RemComp<Content.Server.Flash.Components.FlashImmunityComponent>(args.OldParent.Value);
        }

        var newParent = args.Transform.ParentUid;
        if (newParent.IsValid() && HasComp<ActionsComponent>(newParent))
        {
            var eyeProt = EnsureComp<EyeProtectionComponent>(newParent);
            eyeProt.ProtectionTime = TimeSpan.FromSeconds(10);
            EnsureComp<Content.Server.Flash.Components.FlashImmunityComponent>(newParent);
        }
    }

    private void OnIRVisionParentChanged(EntityUid uid, CyberwareIRVisionComponent component, ref EntParentChangedMessage args)
    {
        if (args.OldParent != null && args.OldParent.Value.IsValid())
        {
            if (TryComp<NightVisionComponent>(args.OldParent.Value, out var oldNv))
                _actions.RemoveAction(args.OldParent.Value, oldNv.ToggleActionEntity);
            
            RemComp<NightVisionComponent>(args.OldParent.Value);
        }

        var newParent = args.Transform.ParentUid;
        if (newParent.IsValid() && HasComp<ActionsComponent>(newParent))
        {
            var nv = EnsureComp<NightVisionComponent>(newParent);
            nv.ToggleAction = "ActionToggleInfraredVision";
            nv.IsEquipment = false;
            
            _actions.AddAction(newParent, ref nv.ToggleActionEntity, nv.ToggleAction, newParent);
        }
    }
}
