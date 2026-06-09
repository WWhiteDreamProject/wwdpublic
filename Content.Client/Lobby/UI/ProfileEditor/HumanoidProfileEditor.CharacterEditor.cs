using Content.Shared.Clothing.Loadouts.Systems;
using Content.Shared.Clothing.Loadouts.Prototypes;


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

        var highJob = _controller.GetPreferredJob(Profile);

        _loadouts.Clear();

        foreach (var loadout in Profile.LoadoutPreferencesList)
        {
            if (!_prototypeManager.TryIndex<LoadoutPrototype>(loadout.LoadoutName, out var loadoutProto))
                continue;

            var usable = _characterRequirementsSystem.CheckRequirementsValid(
                loadoutProto.Requirements,
                highJob,
                Profile,
                _requirements.GetRawPlayTimeTrackers(),
                _requirements.IsWhitelisted(),
                loadoutProto,
                _entManager,
                _prototypeManager,
                _cfgManager,
                out _
            );

            _loadouts.Add(loadoutProto, usable);
        }

        UpdateLoadoutsRemoveButton();

        Loadouts.SetData(
            Profile.LoadoutPreferencesList,
            new(highJob, Profile, _requirements.GetRawPlayTimeTrackers(), _requirements.IsWhitelisted()));

        Loadouts.ShowUnusable = LoadoutsShowUnusableButton.Pressed;
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

        Profile = Profile.WithLoadoutPreference(loadouts);
        ReloadProfilePreview();
        ReloadClothes();
        UpdateLoadouts();
    }
}
