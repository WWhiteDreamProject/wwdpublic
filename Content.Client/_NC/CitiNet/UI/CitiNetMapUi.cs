using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._NC.CitiNet.UI;

/// <summary>
/// Glue class for the CitiNet Map cartridge UI fragment.
/// Registered in prototypes via !type:CitiNetMapUi.
/// </summary>
public sealed partial class CitiNetMapUi : UIFragment
{
    private CitiNetMapUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CitiNetMapUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        _fragment?.UpdateState(state);
    }
}
