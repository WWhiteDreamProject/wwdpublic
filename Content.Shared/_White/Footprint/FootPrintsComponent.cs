using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.FootPrint;

[RegisterComponent]
public sealed partial class FootPrintsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public ResPath RsiPath = new("/Textures/_White/Effects/footprints.rsi");

    // all of those are set as a layer
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string LeftBarePrint = "footprint-left-bare-human";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string RightBarePrint = "footprint-right-bare-human";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string ShoesPrint = "footprint-shoes";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string SuitPrint = "footprint-suit";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] DraggingPrint =
    [
        "dragging-1",
        "dragging-2",
        "dragging-3",
        "dragging-4",
        "dragging-5",
    ];
    // yea, those

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public EntProtoId<FootPrintComponent> StepProtoId = "Footstep";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public Color PrintsColor = Color.FromHex("#00000000");

    [DataField]
    public float StepSize = 0.7f;

    [DataField]
    public float DragSize = 0.5f;

    [DataField]
    public float ColorQuantity;

    [DataField]
    public float ColorReduceAlpha = 0.1f;

    [DataField]
    public string? ReagentToTransfer;

    [DataField]
    public Vector2 OffsetPrint = new(0.1f, 0f);

    public bool RightStep = true;

    public Vector2 StepPos = Vector2.Zero;
}
