using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent]
public sealed partial class PolymorphPowerComponent : Component
{
    [ViewVariables]
    public string OriginalName = "Jonh Doe";

    [ViewVariables]
    public string OriginalDescription = "Killer of the death";

    [ViewVariables]
    public HumanoidCharacterProfile? OriginalProfile = default!;
}
