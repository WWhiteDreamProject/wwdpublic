using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Xenomorphs.Components;

/// <summary>
/// The AlienEggHatchComponent is used to manage the hatching behavior of alien eggs.
/// </summary>
[RegisterComponent]
public sealed partial class XenomorphEggHatchComponent : Component
{
    /// <summary>
    /// Prototype ID for the polymorph effect.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> PolymorphPrototype;

    /// <summary>
    /// Range within which the hatching can be activated.
    /// </summary>
    [DataField]
    public float ActivationRange = 1f;
}
