using Content.Shared._NC.Netrunning.UI;
using Robust.Server.GameObjects;

namespace Content.Server._NC.Netrunning.UI;

public sealed class HackingBoundUserInterface : BoundUserInterface
{
    public HackingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}
