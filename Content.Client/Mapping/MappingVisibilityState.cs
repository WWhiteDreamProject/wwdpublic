using Robust.Client.Graphics;

namespace Content.Client.Mapping;

public sealed class MappingVisibilityState
{
    public bool EntitiesVisible { get; set; } = true;
    public bool TilesVisible { get; set; } = true;
    public bool DecalsVisible { get; set; } = true;
} 