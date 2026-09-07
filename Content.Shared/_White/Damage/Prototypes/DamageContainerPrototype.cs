using Robust.Shared.Prototypes;

namespace Content.Shared._White.Damage.Prototypes;

/// <summary>
/// A damage container which can be used to specify support for various damage types.
/// </summary>
[Prototype]
public sealed partial class DamageContainerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Partial list of damage types supported by this container.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> Types = new();
}
