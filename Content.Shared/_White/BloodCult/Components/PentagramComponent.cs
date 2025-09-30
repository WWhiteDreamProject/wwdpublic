using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._White.BloodCult.Components;

[NetworkedComponent, RegisterComponent]
public sealed partial class PentagramComponent : Component
{
    public ResPath RsiPath = new("/Textures/_White/Effects/pentagram.rsi");

    public readonly string[] States =
    [
        "halo1",
        "halo2",
        "halo3",
        "halo4",
        "halo5",
        "halo6"
    ];
}
