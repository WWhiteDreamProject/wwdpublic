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
        new() { Prototype = "SpawnMobConstructArtificerEffect", },
        new() { Prototype = "SpawnMobConstructJuggernautEffect", },
        new() { Prototype = "SpawnMobConstructWraithEffect", }
    };

    [DataField]
    public List<RadialSelectorEntry> PurifiedConstructs = new()
    {
        new() { Prototype = "SpawnMobConstructArtificerHolyEffect", },
        new() { Prototype = "SpawnMobConstructJuggernautHolyEffect", },
        new() { Prototype = "SpawnMobConstructWraithHolyEffect", }
    };
}
