using Content.Shared._White.CustomGhostSystem;
using Content.Shared.Ghost;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the index of the currently selected character.
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, ICharacterProfile> _characters;

        public PlayerPreferences(IEnumerable<KeyValuePair<int, ICharacterProfile>> characters, int selectedCharacterIndex, Color adminOOCColor, ProtoId<CustomGhostPrototype> ghostPrototype) // WWDP EDIT
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            SelectedCharacterIndex = selectedCharacterIndex;
            AdminOOCColor = adminOOCColor;
            CustomGhost = ghostPrototype; // WWDP EDIT
        }

        // WWDP EDIT START
        public PlayerPreferences WithCharacters(IEnumerable<KeyValuePair<int, ICharacterProfile>> characters) =>
            new(characters, SelectedCharacterIndex, AdminOOCColor, CustomGhost);

        public PlayerPreferences WithSlot(int slot) =>
                    new(_characters, slot, AdminOOCColor, CustomGhost);

        public PlayerPreferences WithAdminOOCColor(Color adminColor) =>
                    new(_characters, SelectedCharacterIndex, adminColor, CustomGhost);

        public PlayerPreferences WithCustomGhost(ProtoId<CustomGhostPrototype> customGhost) =>
                    new(_characters, SelectedCharacterIndex, AdminOOCColor, customGhost);
        // WWDP EDIT END

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];

        public Color AdminOOCColor { get; set; }
        public ProtoId<CustomGhostPrototype> CustomGhost { get; set; } // WWDP EDIT

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }
    }
}
