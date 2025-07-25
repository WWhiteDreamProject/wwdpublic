using Content.Client.Alerts;
using Content.Client.Administration.Managers;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Changeling;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Robust.Client.Player;

namespace Content.Client.Changeling;

public sealed class ChangelingSystem : SharedChangelingSystem
{

    private const int MaxChemicalsNormalizer = 18;
    private const int MaxBiomassNormalizer = 16;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
        SubscribeLocalEvent<ChangelingComponent, GetStatusIconsEvent>(GetChanglingIcon);
    }

    private void OnUpdateAlert(EntityUid uid, ChangelingComponent comp, ref UpdateAlertSpriteEvent args)
    {
        var stateNormalized = 0f;

        // hardcoded because uhh umm i don't know. send help.
        switch (args.Alert.AlertKey.AlertType)
        {
            case "ChangelingChemicals":
                stateNormalized = (int) (comp.Chemicals / comp.MaxChemicals * MaxChemicalsNormalizer);
                break;

            default:
                return;
        }
        var sprite = args.SpriteViewEnt.Comp;
        sprite.LayerSetState(AlertVisualLayers.Base, $"{stateNormalized}");
    }

    [Dependency] private readonly IClientAdminManager _adminManager = default!; // WWDP
    [Dependency] private readonly IPlayerManager _playerManager = default!; // WWDP
    
    private void GetChanglingIcon(Entity<ChangelingComponent> ent, ref GetStatusIconsEvent args)
    {
        // WWDP edit start
        // Check if the local player is an admin
        var isAdmin = _adminManager.GetAdminData() != null;
        
        // Check if the local player is a changeling
        var isChangeling = _playerManager.LocalEntity.HasValue && HasComp<ChangelingComponent>(_playerManager.LocalEntity.Value);

        // Show icon only to admins/admin ghosts, but not to other changelings
        if (!isChangeling && (isAdmin && HasComp<HivemindComponent>(ent)) && _prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
        // WWDP edit end
            args.StatusIcons.Add(iconPrototype);
    }
}
