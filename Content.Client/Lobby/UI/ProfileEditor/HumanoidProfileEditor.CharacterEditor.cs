using System.Linq;
using System.Numerics;
using Content.Shared._White.CharacterEditor;
using Content.Shared.CCVar;
using Content.Shared.Clothing.Loadouts.Prototypes;
using Content.Shared.Clothing.Loadouts.Systems;
using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;


namespace Content.Client.Lobby.UI;


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

        Loadouts.SetData(
            Profile.LoadoutPreferencesList,
            new(
                highJob,
                Profile,
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

        Profile = Profile.WithLoadoutPreference(loadouts);
        ReloadProfilePreview();
        ReloadClothes();
        UpdateLoadouts();
    }
}
