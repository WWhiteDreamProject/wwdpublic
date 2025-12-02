using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganComponent : Component
{
    /// <summary>
    /// Relevant body this organ is attached to.
    /// </summary>
    [DataField]
    public EntityUid? Body;

    /// <summary>
    /// Parent body part for this organ.
    /// </summary>
    [DataField]
    public EntityUid? ParentPart;

    [DataField("organType")] // It can't support the "type" tag.
    public OrganType Type = OrganType.None;
}

/// <summary>
/// Defines various types of organs that can be manipulated.
/// </summary>
[Flags]
public enum OrganType
{
    None = 0,

    /// <summary>
    /// The brain, the central organ of the nervous system.
    /// </summary>
    Brain = 1 << 0,

    /// <summary>
    /// The heart, responsible for pumping blood throughout the body.
    /// </summary>
    Heart = 1 << 1,

    /// <summary>
    /// The eyes, used for sight.
    /// </summary>
    Eyes = 1 << 2,

    /// <summary>
    /// The tongue, used for speech.
    /// </summary>
    Tongue = 1 << 3,

    /// <summary>
    /// The appendix, used for explosion.
    /// </summary>
    Appendix = 1 << 4,

    /// <summary>
    /// The ears, used for hearing.
    /// </summary>
    Ears = 1 << 5,

    /// <summary>
    /// The lungs, used for respirating.
    /// </summary>
    Lungs = 1 << 6,

    /// <summary>
    /// The stomach, used for digesting.
    /// </summary>
    Stomach = 1 << 7,

    /// <summary>
    /// The liver, used for metabolism.
    /// </summary>
    Liver = 1 << 8,

    /// <summary>
    /// The kidneys, used for blood filtering.
    /// </summary>
    Kidneys = 1 << 9,

    /// <summary>
    /// The infection, used for introduce infection/disease into the body.
    /// </summary>
    Infection = 1 << 10,

    /// <summary>
    /// The gland, used to isolate various substances.
    /// </summary>
    Gland = 1 << 11,

    Specific = 1 << 12,

    /// <summary>
    /// The core, used for the metabolization of reagents, is also the center of the nervous system.
    /// </summary>
    Core = Brain | Heart | Stomach | Liver | Kidneys
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class OrganSlot
{
    public OrganSlot() { }

    public OrganSlot(OrganSlot other)
    {
        Type = other.Type;
        StartingOrgan = other.StartingOrgan;
    }

    public OrganSlot(OrganType type, EntProtoId? startingOrgan)
    {
        Type = type;
        StartingOrgan = startingOrgan;
    }

    [DataField]
    public OrganType Type = OrganType.None;

    [DataField(readOnly: true)]
    public EntProtoId? StartingOrgan;

    [ViewVariables, NonSerialized]
    public ContainerSlot? ContainerSlot;

    public string? Id => ContainerSlot?.ID;
    public bool HasOrgan => ContainerSlot?.ContainedEntity != null;
    public EntityUid? OrganUid => ContainerSlot?.ContainedEntity;
}
