using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Decals;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Crayon;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, ComponentInit>(OnCrayonInit);
        // SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonBoundUI);     // WWDP EDIT - Moved to Shared.
        // SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonBoundUIColor); // WWDP EDIT - Moved to Shared.
        SubscribeLocalEvent<CrayonComponent, UseInHandEvent>(OnCrayonUse, before: new[] { typeof(FoodSystem) });
        SubscribeLocalEvent<CrayonComponent, AfterInteractEvent>(OnCrayonAfterInteract, after: new[] { typeof(FoodSystem) });
        // SubscribeLocalEvent<CrayonComponent, DroppedEvent>(OnCrayonDropped); // WWDP EDIT - Moved to Shared.
        // SubscribeLocalEvent<CrayonComponent, ComponentGetState>(OnCrayonGetState); // WWDP EDIT - DEFUNCT - Moved to using AutoState system.
    }

    /* WWDP EDIT - DEFUNCT - Moved to using AutoState system.
    private static void OnCrayonGetState(EntityUid uid, CrayonComponent component, ref ComponentGetState args)
    {
        args.State = new CrayonComponentState(component.Color, component.SelectedState, component.Charges, component.Capacity);
    }
    */

    private void OnCrayonAfterInteract(EntityUid uid, CrayonComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (component.Charges <= 0)
        {
            if (component.DeleteEmpty)
                UseUpCrayon(uid, args.User);
            else
                _popup.PopupEntity(Loc.GetString("crayon-interact-not-enough-left-text"), uid, args.User);

            args.Handled = true;
            return;
        }

        if (!args.ClickLocation.IsValid(EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("crayon-interact-invalid-location"), uid, args.User);
            args.Handled = true;
            return;
        }
        
        // WWDP EDIT START
        Angle rot = 0f;
        if (TryComp<InputMoverComponent>(args.User, out var mover))
        {
            rot = mover.TargetRelativeRotation;
        }

        if (!_decals.TryAddDecal(component.SelectedState, args.ClickLocation.Offset(new Vector2(-0.5f, -0.5f)), out _, component.Color, rot + component.Angle + 0.01f, cleanable: true))
            return;
        // WWDP EDIT END
        
        if (component.UseSound != null)
            _audio.PlayPvs(component.UseSound, uid, AudioParams.Default.WithVariation(0.125f));

        // Frontier: check if crayon is infinite, Delta V Port
        if (component.Charges != int.MaxValue)
        {
            // Decrease "Ammo"
            component.Charges--;
            Dirty(uid, component);
        }
        // End Frontier, Delta V Port

        _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{EntityManager.ToPrettyString(args.User):user} drew a {component.Color:color} {component.SelectedState}");
        args.Handled = true;

        if (component.DeleteEmpty && component.Charges <= 0)
            UseUpCrayon(uid, args.User);
        else
            _uiSystem.ServerSendUiMessage(uid, CrayonComponent.CrayonUiKey.Key, new CrayonUsedMessage(component.SelectedState)); // WWDP EDIT
    }

    private void OnCrayonUse(EntityUid uid, CrayonComponent component, UseInHandEvent args)
    {
        // Open crayon window if neccessary.
        if (args.Handled)
            return;

        if (!_uiSystem.HasUi(uid, CrayonComponent.CrayonUiKey.Key)) // WWDP EDIT
        {
            return;
        }

        _uiSystem.TryToggleUi(uid, CrayonComponent.CrayonUiKey.Key, args.User); // WWDP EDIT

        _uiSystem.SetUiState(uid, CrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.SelectedState, component.SelectableColor, component.Color)); // WWDP EDIT
        args.Handled = true;
    }

    private void OnCrayonInit(EntityUid uid, CrayonComponent component, ComponentInit args)
    {
        component.Charges = component.Capacity;

        // Get the first one from the catalog and set it as default
        var decal = _prototypeManager.EnumeratePrototypes<DecalPrototype>().FirstOrDefault(x => x.Tags.Contains("crayon"));
        component.SelectedState = decal?.ID ?? string.Empty;
        Dirty(uid, component);
    }


    private void UseUpCrayon(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), user, user);
        EntityManager.QueueDeleteEntity(uid);
    }
}
