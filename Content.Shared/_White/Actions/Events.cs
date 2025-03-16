using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Actions;

/// <summary>
/// Event for the instant action related to weed node actions.
/// </summary>
public sealed partial class WeednodeActionEvent : InstantActionEvent;

public sealed partial class SpawnTileEntityActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId? Entity;

    [DataField]
    public string? TileId;

    [DataField]
    public SoundSpecifier? Audio;

}
