using Robust.Shared.Prototypes;

namespace Content.Server._White.Progenitor.Components;

[RegisterComponent]
public sealed partial class ProgenitorProviderComponent : Component
{
    /// <summary>
    /// Whether to transfer the mind to this new entity.
    /// </summary>
    [DataField]
    public bool TransferMind;

    /// <summary>
    /// The entity to replace the provider with.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
}
