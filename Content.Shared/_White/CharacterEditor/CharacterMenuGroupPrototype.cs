using Content.Shared.Clothing.Loadouts.Prototypes;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.CharacterEditor;

[Prototype]
public sealed class CharacterMenuRootPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField] public CharacterMenuGroup Root = default!;
}

[DataDefinition]
public sealed partial class CharacterMenuGroup
{
    [DataField] public string Name = default!;
    [DataField] public List<CharacterMenuGroup> SubGroups = [];
    [DataField] public List<MarkingCategories> Categories = [];
    [DataField] public List<ProtoId<LoadoutCategoryPrototype>> LoadoutCategories = [];
}
