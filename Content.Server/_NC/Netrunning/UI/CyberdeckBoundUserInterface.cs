using Content.Shared._NC.Netrunning.Components;
using Robust.Server.GameObjects;

namespace Content.Server._NC.Netrunning.UI;

public sealed class CyberdeckBoundUserInterface : BoundUserInterface
{
    public CyberdeckBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}
