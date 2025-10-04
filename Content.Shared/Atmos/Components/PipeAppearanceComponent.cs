using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

[RegisterComponent, NetworkedComponent]
public sealed partial class PipeAppearanceComponent : Component
{
    [DataField("sprite")]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(
        new("Structures/Piping/Atmospherics/pipe.rsi"),
        "pipeConnector");

    [DataField("connectors")]
    public Dictionary<string, ConnectorSprite> Connectors = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ConnectorSprite
{
    [DataField("sprite", required: true)]
    public SpriteSpecifier Sprite = default!;

    [DataField("targetTypes")]
    public List<string> TargetTypes = new();
}
