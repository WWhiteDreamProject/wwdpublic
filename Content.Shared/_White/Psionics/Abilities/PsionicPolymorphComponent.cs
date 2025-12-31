using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent]
public sealed partial class PolymorphPowerComponent : Component
{
    [DataField, ViewVariables]
    public string OriginalName = string.Empty;

    [DataField, ViewVariables]
    public string OriginalDescription = string.Empty;

    [DataField, ViewVariables]
    public HumanoidCharacterProfile? OriginalProfile;
}
