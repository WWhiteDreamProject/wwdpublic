using Robust.Shared.Prototypes;

namespace Content.Server._White.GameTicking.Components;

[RegisterComponent]
public sealed partial class DeleteSpawnOnGameruleComponent : Component
{
    [DataField("tilesToRemove")]
    public List<string> TilesToRemove = new();

    [DataField("entityTagsToRemove")]
    public List<string> EntityTagsToRemove = new();

    [DataField("markersToActivate")]
    public List<string> MarkersToActivate = new();
}
