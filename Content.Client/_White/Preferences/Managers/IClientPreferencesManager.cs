using Content.Shared._White.Preferences;
using Content.Shared.Preferences;

namespace Content.Client._White.Preferences.Managers;

public interface IClientPreferencesManager
{
    event Action OnServerDataLoaded;

    bool ServerDataLoaded => Settings != null;

    GameSettings? Settings { get; }
    PlayerPreferences? Preferences { get; }
    void Initialize();
    void SelectCharacter(HumanoidCharacterProfile profile);
    void SelectCharacter(int slot);
    void UpdateCharacter(HumanoidCharacterProfile profile, int slot);
    void CreateCharacter(HumanoidCharacterProfile profile);
    void DeleteCharacter(HumanoidCharacterProfile profile);
    void DeleteCharacter(int slot);
}
