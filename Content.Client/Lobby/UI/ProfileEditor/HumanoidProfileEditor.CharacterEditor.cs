using System.Linq;
using Content.Shared.Clothing.Loadouts.Systems;


namespace Content.Client.Lobby.UI;

// WWDP PARTIAL CLASS
public sealed partial class HumanoidProfileEditor
{
    private void InitializeCharacterMenu()
    {
        Loadouts.OnLoadoutsChanged += OnLoadoutsChange;
    }

    private void UpdateLoadouts()
    {
        if (Profile == null)
            return;

        var highJob = _controller.GetPreferredJob(Profile.Value);

        Loadouts.SetData(
            Profile.Value.Loadouts.Values,
            new(
                highJob,
                Profile.Value,
                _requirements.GetRawPlayTimeTrackers(),
                _requirements.IsWhitelisted()
                )
            );
    }

    private void CheckpointLoadouts()
    {
        if (Profile == null)
            return;
        Loadouts.SetCheckpoint();
    }

    private void OnLoadoutsChange(List<Loadout> loadouts)
    {
        if (Profile is null)
            return;

        Profile = Profile.Value.WithLoadout(loadouts.ToDictionary(x => x.LoadoutName));
        ReloadProfilePreview();
        ReloadClothes();
        UpdateLoadouts();
    }
}
