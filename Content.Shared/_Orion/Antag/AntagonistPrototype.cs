using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Antag;

[Prototype("antagonist")]
public sealed partial class AntagonistPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    ///     Name string to display in ghost teleport menu .
    /// </summary>
    [DataField(required: true)]
    public string Name = default!;

    /// <summary>
    ///     Description string to display in the ghost teleport menu as an explanation of the antagonist's role.
    /// </summary>
    [DataField(required: true)]
    public string Description = default!;

    /// <summary>
    ///     Weight value for sorting antagonists in the ghost teleport menu. Higher values appear first.
    /// </summary>
    [DataField(required: true)]
    public int Weight;
}
