using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent]
public sealed partial class PolymorphPowerComponent : Component
{
    [DataField, ViewVariables]]
    public string OriginalName = "Jonh Doe";

    [DataField, ViewVariables]
    public string OriginalDescription = "Killer of the death";

    [DataField, ViewVariables]
    public HumanoidCharacterProfile? OriginalProfile;
}
