using Robust.Shared.Prototypes;

namespace Content.Shared._White.Antag;

[Prototype("antagonist")]
public sealed partial class AntagonistPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    ///     A name  string to display in ghost teleport menu .
    /// </summary>
    [DataField(required: true)]
    public string Name = default!;

    /// <summary>
    ///     A description string to display in the character menu as an explanation of the department's function.
    /// </summary>
    [DataField(required: true)]
    public string Description = default!;

    /// <summary>
    ///     A description string to display in the character menu as an explanation of the department's function.
    /// </summary>
    [DataField(required: true)]
    public int Weight = default!;
}
