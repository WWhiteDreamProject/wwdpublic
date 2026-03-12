using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.CitiNet;

[Serializable, NetSerializable, Prototype("citiNetMapConfig")]
public sealed class CitiNetMapConfigPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Mapping of Job IDs to their map visibility permissions.
    /// Key: Job ID (e.g., "NCPDDispatcher")
    /// Value: List of beacon groups they can see.
    /// </summary>
    [DataField("roleConfigs")]
    public Dictionary<string, List<string>> RoleConfigs { get; private set; } = new();
}
