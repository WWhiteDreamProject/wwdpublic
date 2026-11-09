using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared._White.Humanoid.Systems;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Components;

/// <summary>
/// Defines the visual appearance and demographic profile of a humanoid character.
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(HumanoidProfileSystem))]
public sealed partial class HumanoidProfileComponent : Component
{
    /// <summary>
    /// The height of this character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Height = 1f;

    /// <summary>
    /// The width of this character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Width = 1f;

    /// <summary>
    /// The gender of the character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Gender Gender;

    /// <summary>
    /// The age of the character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Age = 18;

    /// <summary>
    /// The body type of the character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BodyTypePrototype> BodyType = HumanoidProfileSystem.DefaultBodyType;

    /// <summary>
    /// The species of the character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species = HumanoidProfileSystem.DefaultSpecies;

    /// <summary>
    /// The sex of the character.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Sex Sex;
}
