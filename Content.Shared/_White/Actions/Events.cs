using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Actions;

public sealed partial class SpawnTileEntityActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId? Entity;

    [DataField]
    public string? TileId;

    [DataField]
    public SoundSpecifier? Audio;
}

public sealed partial class PlaceTileEntityEvent : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId? Entity;

    [DataField]
    public string? TileId;

    [DataField]
    public SoundSpecifier? Audio;

    [DataField]
    public float Length;
}

[Serializable, NetSerializable]
public sealed partial class PlaceTileEntityDoAfterEvent : DoAfterEvent
{
    public NetCoordinates Target;

    public EntProtoId? Entity;

    public string? TileId;

    public SoundSpecifier? Audio;

    public override DoAfterEvent Clone() => this;
}
