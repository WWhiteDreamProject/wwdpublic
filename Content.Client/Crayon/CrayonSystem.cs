using Content.Client._White.Overlays;
using Content.Client.Hands.Systems;
using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Crayon;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    // Didn't do in shared because I don't think most of the server stuff can be predicted. // if i had arms long enough to reach around the globe i would strangle you.
    public override void Initialize()
    {
        base.Initialize();
        // SubscribeLocalEvent<CrayonComponent, ComponentHandleState>(OnCrayonHandleState); // WWDP EDIT - DEFUNCT - Moved to using AutoState system.
        Subs.ItemStatus<CrayonComponent>(ent => new StatusControl(ent));

        // WWDP EDIT START
        _overlay.AddOverlay(new CrayonPreviewOverlay(_sprite, _hands));
        SubscribeLocalEvent<CrayonComponent, AfterAutoHandleStateEvent>(CrayonAfterAutoState);
        SubscribeLocalEvent<CrayonComponent, AfterInteractEvent>(CrayonAfterInteract);
        //SubscribeLocalEvent<CrayonComponent, ComponentRemove>(CrayonRemoved);
        //SubscribeLocalEvent<CrayonComponent, EntityTerminatingEvent>(CrayonEntRemoved);
        // WWDP EDIT END
    }

    // WWDP EDIT START
    private void CrayonAfterInteract(EntityUid uid, CrayonComponent comp, AfterInteractEvent args)
    {
        if (comp.Charges <= 1)
            _ui.CloseUi(uid, CrayonComponent.CrayonUiKey.Key, args.User);
    }

    // Some interactions involve removing an item from active hand without raising HandDeselectedEvent on the item first.
    // I do not want to hunt down all the possible instances of this behaviour, so i am plugging the hole by making the
    // overlay always on.
    //private void CrayonRemoved(EntityUid uid, CrayonComponent comp, ComponentRemove args)
    //{
    //    if (_player.LocalEntity is not EntityUid player ||
    //        uid != _hands.GetActiveItem(player))
    //        return;
    //    _overlay.RemoveOverlay<CrayonPreviewOverlay>();
    //}
    //
    //private void CrayonEntRemoved(EntityUid uid, CrayonComponent comp, EntityTerminatingEvent args)
    //{
    //    if (_player.LocalEntity is not EntityUid player ||
    //        uid != _hands.GetActiveItem(player))
    //        return;
    //    _overlay.RemoveOverlay<CrayonPreviewOverlay>();
    //}

    private void CrayonAfterAutoState(EntityUid uid, CrayonComponent comp, AfterAutoHandleStateEvent args)
    {
        comp.UIUpdateNeeded = true;
    }

    //protected override void OnCrayonHandSelected(EntityUid uid, CrayonComponent component, HandSelectedEvent args) // WWDP EDIT
    //{
    //    base.OnCrayonHandSelected(uid, component, args);
    //    _overlay.RemoveOverlay<CrayonPreviewOverlay>(); // if i still fucked up somewhere and did not remove the event when i should've, this will catch it.
    //    _overlay.AddOverlay(new CrayonPreviewOverlay(_sprite, component));
    //}
    //
    //protected override void OnCrayonHandDeselected(EntityUid uid, CrayonComponent component, HandDeselectedEvent args) // WWDP EDIT
    //{
    //    base.OnCrayonHandDeselected(uid, component, args);
    //    _overlay.RemoveOverlay<CrayonPreviewOverlay>();
    //}
    // WWDP EDIT END

    private sealed class StatusControl : Control
    {
        private readonly CrayonComponent _parent;
        private readonly RichTextLabel _label;

        public StatusControl(CrayonComponent parent)
        {
            _parent = parent;
            _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
            AddChild(_label);

            parent.UIUpdateNeeded = true;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_parent.UIUpdateNeeded)
            {
                return;
            }

            _parent.UIUpdateNeeded = false;

            // Frontier: unlimited crayon, Delta V Port
            if (_parent.Capacity == int.MaxValue)
            {
                _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label-unlimited",
                    ("color", _parent.Color),
                    ("state", _parent.SelectedState)));
                return;
            }
            // End Frontier, Delta V Port

            _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("crayon-drawing-label",
                ("color",_parent.Color),
                ("state",_parent.SelectedState),
                ("charges", _parent.Charges),
                ("capacity",_parent.Capacity)));
        }
    }
}
