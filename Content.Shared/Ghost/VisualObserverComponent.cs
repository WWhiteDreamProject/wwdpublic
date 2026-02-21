using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VisualObserverComponent : Component
{
    [DataField("shaderName"), AutoNetworkedField]
    public string ShaderName = "GhostCompositeTint";

    [DataField("alphaMultiplier"), AutoNetworkedField]
    public float AlphaMultiplier = 0.5f;

    [DataField("fallbackPrototype")]
    public bool FallbackPrototype;

    [DataField("snapshotSlots")]
    public List<string> SnapshotSlots = new()
    {
        "shoes",
        "jumpsuit",
        "outerClothing",
        "gloves",
        "neck",
        "mask",
        "eyes",
        "ears",
        "head",
        "id",
        "belt",
        "back",
        "suitstorage",
        "innerBelt",
        "innerNeck"
    };
}
