using Content.Shared._White.CustomGhostSystem;
using Content.Shared.Ghost;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using System;

namespace Content.Client.Lobby
{
    public interface IClientPreferencesManager
    {
        event Action OnServerDataLoaded;

        bool ServerDataLoaded => Settings != null;

        GameSettings? Settings { get; }
        PlayerPreferences? Preferences { get; }
        void Initialize();
        void SelectCharacter(ICharacterProfile profile);
        void SelectCharacter(int slot);
        void UpdateCharacter(ICharacterProfile profile, int slot);
        void CreateCharacter(ICharacterProfile profile);
        void DeleteCharacter(ICharacterProfile profile);
        void DeleteCharacter(int slot);
        void SetCustomGhost(ProtoId<CustomGhostPrototype> ghostProto); // WWDP EDIT
    }
}
