using Content.Shared.Containers.ItemSlots;
using Content.Shared.RadialSelector;

namespace Content.Server._White.BloodCult.Constructs.Shell;

[RegisterComponent]
public sealed partial class ConstructShellComponent : Component
{
    [DataField(required: true)]
    public ItemSlot ShardSlot = new();

    public readonly string ShardSlotId = "Shard";

    [DataField]
    public List<RadialSelectorEntry> Constructs = new()
    {
        new() { Prototype = "MobConstructArtificer", },
        new() { Prototype = "MobConstructJuggernaut", },
        new() { Prototype = "MobConstructWraith", }
    };

    [DataField]
    public List<RadialSelectorEntry> PurifiedConstructs = new()
    {
        new() { Prototype = "MobConstructArtificerHoly", },
        new() { Prototype = "MobConstructJuggernautHoly", },
        new() { Prototype = "MobConstructWraithHoly", }
    };
}
