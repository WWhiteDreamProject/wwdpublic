using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

[Serializable, NetSerializable, Prototype("ncStoreListing")]
public sealed class StoreListingPrototype : IPrototype
{
    [IdDataField] public string Id = string.Empty;

    [DataField("match")] public PrototypeMatchMode MatchMode = PrototypeMatchMode.Exact;
    [DataField("mode")] public StoreMode Mode = StoreMode.Buy;

    [DataField("productEntity")] public string ProductEntity = string.Empty;

    [DataField("cost")] public Dictionary<string, int> Cost { get; set; } = new();

    [DataField("categories")] public List<string> Categories { get; set; } = new();

    [DataField("conditions")] public List<ListingConditionPrototype> Conditions { get; set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public int RemainingCount { get; set; } = -1;

    public string ID => Id;
}

[DataDefinition]
public sealed partial class StoreCatalogEntry
{
    [DataField("match")] public PrototypeMatchMode MatchMode = PrototypeMatchMode.Exact;
    [DataField("price", required: true)] public int Price;
    [DataField("proto", required: true)] public string Proto = string.Empty;
    [DataField("count")] public int? Count { get; set; }
    [DataField("amount")] public int Amount { get; set; } = 1;
}

[Prototype("storeCategoryStructured")]
public sealed partial class StoreCategoryStructuredPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField("entries", required: true)]
    public List<StoreCatalogEntry> Entries { get; private set; } = new();
}

[Prototype("storePresetStructured")]
public sealed partial class StorePresetStructuredPrototype : IPrototype
{
    [DataField("categories", required: true)]
    public List<string> Categories { get; private set; } = new();

    [DataField("currency", required: true)]
    public string Currency = string.Empty;

    [IdDataField]
    public string ID { get; private set; } = default!;
}


[Prototype("storeContract")]
public sealed class StoreContractPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("match")] public PrototypeMatchMode MatchMode { get; private set; } = PrototypeMatchMode.Exact;

    [DataField("name")] public string Name { get; private set; } = string.Empty;
    [DataField("description")] public string Description { get; private set; } = string.Empty;

    [DataField("difficulty")] public string Difficulty { get; private set; } = "Easy";
    [DataField("repeatable")] public bool Repeatable { get; private set; } = true;

    [DataField("targetItem")] public string? TargetItem { get; private set; }

    [DataField("required")] public IntRange Required { get; private set; } = IntRange.Fixed(0);

    [DataField("targets")] public List<StoreContractTargetEntry>? Targets { get; private set; }

    [DataField("targetCount")] public IntRange TargetCount { get; private set; } = IntRange.Fixed(1);

    [DataField("rewards")]
    public List<ContractRewardDef> Rewards { get; private set; } = new();
}

[DataDefinition]
public sealed partial class StoreContractTargetEntry
{
    [DataField("id", required: true)] public string TargetItemId { get; set; } = default!;
    [DataField("required")] public IntRange Required { get; set; } = IntRange.Fixed(0);
    [DataField("weight")] public int Weight { get; set; } = 1;
}

[Prototype("storeContractsPreset")]
public sealed class StoreContractsPresetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("limits", required: true)]
    public Dictionary<string, int> Limits { get; set; } = new();

    [DataField("packs")]
    public List<PackIncludeEntry> Packs { get; set; } = new();
}

[DataDefinition]
public partial struct ContractWeightEntry
{
    [DataField("id", required: true)] public string Id = string.Empty;
    [DataField("weight")] public int Weight = 1;

    public ContractWeightEntry(string id, int weight)
    {
        Id = id;
        Weight = weight;
    }
}

[DataDefinition]
public partial struct PackIncludeEntry
{
    [DataField("id", required: true)] public string Id = string.Empty;
    [DataField("weight")] public int Weight = 1;

    public PackIncludeEntry(string id, int weight)
    {
        Id = id;
        Weight = weight;
    }
}

[Prototype("storeContractPack")]
public sealed class StoreContractPackPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("contracts")]
    public List<ContractWeightEntry> Contracts { get; set; } = new();

    [DataField("includes")]
    public List<PackIncludeEntry> Includes { get; set; } = new();
}



[Prototype("ncContractRewardPool")]
public sealed class NcContractRewardPoolPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("entries")]
    public List<ContractRewardDef> Entries { get; private set; } = new();
}



[Serializable, NetSerializable]
public enum PrototypeMatchMode : byte
{
    Exact = 0,
    Descendants = 1
}

[Serializable]
public sealed class ListingConditionPrototype
{
    [DataField("condition")]
    public object? Condition;
}
