using System.Linq;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Crayon.UI
{
    public sealed class CrayonBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        [ViewVariables]
        private CrayonWindow? _menu;
        [ViewVariables]                      // WWDP EDIT
        private CrayonComponent? _ownerComp; // WWDP EDIT

        public CrayonBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            EntMan.TryGetComponent<CrayonComponent>(owner, out _ownerComp); // WWDP EDIT
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<CrayonWindow>();
            _menu.OnColorSelected += SelectColor;
            _menu.OnSelected += Select;
            PopulateCrayons();
            _menu.OpenCenteredLeft();
            //_menu.Search.GrabKeyboardFocus();
        }

        private void PopulateCrayons()
        {
			// WWDP EDIT START
            var crayonDecals = _protoManager.EnumeratePrototypes<DecalPrototype>();
            if(_ownerComp?.AllDecals != true)
                crayonDecals = crayonDecals.Where(x => x.Tags.Contains("crayon"));
            // WWDP EDIT END
            _menu?.Populate(crayonDecals.ToList());
        }

        public override void OnProtoReload(PrototypesReloadedEventArgs args)
        {
            base.OnProtoReload(args);

            if (!args.WasModified<DecalPrototype>())
                return;

            PopulateCrayons();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (_menu is null || message is not CrayonUsedMessage crayonMessage)
                return;

            _menu.AdvanceState(crayonMessage.DrawnDecal);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _menu?.UpdateState((CrayonBoundUserInterfaceState) state);
        }

        public void Select(string state)
        {
            SendPredictedMessage(new CrayonSelectMessage(state));
        }

        public void SelectColor(Color color)
        {
            SendPredictedMessage(new CrayonColorMessage(color));
        }
    }
}
