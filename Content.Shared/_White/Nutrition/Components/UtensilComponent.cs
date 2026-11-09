using Content.Shared._White.Nutrition.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Nutrition.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedIngestionSystem))]
public sealed partial class UtensilComponent : Component
{
    /// <summary>
    /// The chance that the utensil has to break with each use.
    /// A value of 0 means that it is unbreakable.
    /// </summary>
    [DataField("breakChance")]
    public float BreakChance;

    /// <summary>
    /// The sound to be played if the utensil breaks.
    /// </summary>
    [DataField("breakSound")]
    public SoundSpecifier BreakSound = new SoundPathSpecifier("/Audio/Items/snap.ogg");

    /// <summary>
    /// Defines the types of utensil this item represents.
    /// </summary>
    [DataField]
    public UtensilType Types = UtensilType.None;
}

/// <summary>
/// Defines the different types of utensils that can exist.
/// </summary>
[Flags]
public enum UtensilType : byte
{
    None = 0,
    Fork = 1,
    Spoon = 1 << 1,
    Knife = 1 << 2,
}
