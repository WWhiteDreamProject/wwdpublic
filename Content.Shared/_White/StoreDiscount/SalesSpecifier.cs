namespace Content.Shared._White.StoreDiscount;

[DataDefinition]
public sealed partial class SalesSpecifier
{
    [DataField("enabled")]
    public bool Enabled { get; private set; }

    [DataField("minMultiplier")]
    public float MinMultiplier { get; private set; }

    [DataField("maxMultiplier")]
    public float MaxMultiplier { get; private set; }

    [DataField("minItems")]
    public int MinItems { get; private set;  }

    [DataField("maxItems")]
    public int MaxItems { get; private set; }

    [DataField("salesCategory")]
    public string SalesCategory { get; private set; } = string.Empty;

    public SalesSpecifier()
    {
    }

    public SalesSpecifier(bool enabled, float minMultiplier, float maxMultiplier, int minItems, int maxItems,
        string salesCategory)
    {
        Enabled = enabled;
        MinMultiplier = minMultiplier;
        MaxMultiplier = maxMultiplier;
        MinItems = minItems;
        MaxItems = maxItems;
        SalesCategory = salesCategory;
    }
}
