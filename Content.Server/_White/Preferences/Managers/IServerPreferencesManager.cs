using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._White.Preferences;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._White.Preferences.Managers;

public interface IServerPreferencesManager
{
    void Initialize();

    Task LoadData(ICommonSession session, CancellationToken cancel);
    void FinishLoad(ICommonSession session);
    void OnClientDisconnected(ICommonSession session);

    bool TryGetPreferences(NetUserId userId, [NotNullWhen(true)] out PlayerPreferences? playerPreferences);
    PlayerPreferences GetPreferences(NetUserId userId);
    PlayerPreferences? GetPreferencesOrNull(NetUserId? userId);
    IEnumerable<KeyValuePair<NetUserId, HumanoidCharacterProfile>> GetSelectedProfilesForPlayers(List<NetUserId> userIds);
    bool HavePreferences(ICommonSession session);

    Task SetProfile(NetUserId userId, int slot, HumanoidCharacterProfile profile);
}
