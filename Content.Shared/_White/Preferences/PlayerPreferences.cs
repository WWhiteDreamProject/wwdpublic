using Content.Shared._White.CustomGhostSystem;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Preferences;

/// <summary>
/// Holds all player character data, including multiple character profiles and the index of the currently selected character.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayerPreferences(
    IEnumerable<KeyValuePair<int, HumanoidCharacterProfile>> characters,
    int selectedCharacterIndex,
    Color adminOOCColor,
    ProtoId<CustomGhostPrototype> customGhost)
{
    /// <summary>
    /// Internal storage for character profiles, keyed by slot index.
    /// </summary>
    private Dictionary<int, HumanoidCharacterProfile> _characters = new(characters);

    /// <summary>
    /// The color used for OOC messages sent by administrators.
    /// </summary>
    public Color AdminOOCColor { get; set; } = adminOOCColor;

    /// <summary>
    /// The index indicating which character profile is currently selected by the player.
    /// </summary>
    public int SelectedCharacterIndex { get; } = selectedCharacterIndex;

    /// <summary>
    /// The prototype ID for the custom ghost appearance.
    /// </summary>
    public ProtoId<CustomGhostPrototype> CustomGhost { get; set; } = customGhost;

    /// <summary>
    /// Retrieves the currently selected character profile based on <see cref="SelectedCharacterIndex"/>.
    /// </summary>
    public HumanoidCharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];

    /// <inheritdoc cref="_characters"/>
    public IReadOnlyDictionary<int, HumanoidCharacterProfile> Characters => _characters;

    /// <summary>
    /// Attempts to find the index of a given character profile within the player's collection.
    /// </summary>
    /// <param name="profile">The character profile to search for.</param>
    /// <param name="index">An output parameter that will contain the index if the profile is found.</param>
    /// <returns>True if the profile was found and its index was assigned to <paramref name="index"/>; otherwise, false.</returns>
    public bool TryIndexOfCharacter(HumanoidCharacterProfile profile, out int index)
    {
        return (index = IndexOfCharacter(profile)) != -1;
    }

    /// <summary>
    /// Retrieves a specific character profile by its slot index.
    /// </summary>
    /// <param name="index">The slot index of the character profile to retrieve.</param>
    /// <returns>The <see cref="HumanoidCharacterProfile"/> at the specified index.</returns>
    public HumanoidCharacterProfile GetProfile(int index)
    {
        return _characters[index];
    }

    /// <summary>
    /// Finds the slot index of a given character profile.
    /// </summary>
    /// <param name="profile">The character profile to find the index for.</param>
    /// <returns>The integer slot index of the profile if found; otherwise, -1.</returns>
    public int IndexOfCharacter(HumanoidCharacterProfile profile)
    {
        return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
    }

    /// <summary>
    /// Creates a new <see cref="PlayerPreferences"/> instance with an updated administrator OOC color.
    /// </summary>
    /// <param name="adminOOCColor">The new color for administrator OOC messages.</param>
    /// <returns>A new <see cref="PlayerPreferences"/> instance with the updated color.</returns>
    public PlayerPreferences WithAdminOOCColor(Color adminOOCColor)
    {
        return new(_characters, SelectedCharacterIndex, adminOOCColor, CustomGhost);
    }

    /// <summary>
    /// Creates a new <see cref="PlayerPreferences"/> instance with a new set of character profiles.
    /// </summary>
    /// <param name="characters">A new collection of character profiles to use.</param>
    /// <returns>A new <see cref="PlayerPreferences"/> instance with the updated characters.</returns>
    public PlayerPreferences WithCharacters(IEnumerable<KeyValuePair<int, HumanoidCharacterProfile>> characters)
    {
        return new(characters, SelectedCharacterIndex, AdminOOCColor, CustomGhost);
    }

    /// <summary>
    /// Creates a new <see cref="PlayerPreferences"/> instance with an updated custom ghost appearance.
    /// </summary>
    /// <param name="customGhost">The new prototype ID for the custom ghost appearance.</param>
    /// <returns>A new <see cref="PlayerPreferences"/> instance with the updated ghost appearance.</returns>
    public PlayerPreferences WithCustomGhost(ProtoId<CustomGhostPrototype> customGhost)
    {
        return new(_characters, SelectedCharacterIndex, AdminOOCColor, customGhost);
    }

    /// <summary>
    /// Creates a new <see cref="PlayerPreferences"/> instance with a different selected character slot.
    /// </summary>
    /// <param name="slot">The new slot index to set as selected.</param>
    /// <returns>A new <see cref="PlayerPreferences"/> instance with the updated selected character index.</returns>
    public PlayerPreferences WithSlot(int slot)
    {
        return new(_characters, slot, AdminOOCColor, CustomGhost);
    }
}
