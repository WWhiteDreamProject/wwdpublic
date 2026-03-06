using Content.Shared._White.Body.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BodyComponent : Component
{
    [DataField]
    public bool ThermalVisibility = true;

    /// <summary>
    /// Body parts attached to this body.
    /// </summary>
    [DataField]
    public Dictionary<string, BodyPartSlot> BodyParts = new();

    /// <summary>
    /// Organs attached to this body.
    /// </summary>
    [DataField]
    public Dictionary<string, OrganSlot> Organs = new();

    /// <summary>
    /// Relevant template to spawn for this body.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BodyPrototype> Prototype;

    [DataField]
    public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");
}

[Serializable, NetSerializable]
public sealed class BodyComponentState : ComponentState
{
    public BodyComponentState(BodyComponent component, EntityManager entityManager)
    {
        BodyParts = new();
        foreach (var (stateKey, stateSlot) in component.BodyParts)
            BodyParts.Add(stateKey, (stateSlot, entityManager.GetNetEntity(stateSlot.ContainerSlot?.Owner)));

        Organs = new();
        foreach (var (stateKey, stateSlot) in component.Organs)
            Organs.Add(stateKey, (stateSlot, entityManager.GetNetEntity(stateSlot.ContainerSlot?.Owner)));
    }

    public readonly Dictionary<string, (BodyPartSlot BodyPartSlot, NetEntity? BodyPartOwner)> BodyParts;

    public readonly Dictionary<string, (OrganSlot OrganSlot, NetEntity? OrganOwner)> Organs;
}
