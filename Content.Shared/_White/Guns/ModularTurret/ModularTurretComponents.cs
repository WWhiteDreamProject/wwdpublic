using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns.ModularTurret;


[RegisterComponent]
public sealed partial class ModularTurretWeaponComponent : Component
{
    [DataField(required: true)]
    public List<string> WeaponClass = new();

    [DataField]
    public bool OnlyUsableByTurret = true;

    [DataField]
    public EntityUid? CurrentTurretHolder;

    [DataField("dollyMixSprite")]
    public string? DollyMixRSIPath = null;
}

[RegisterComponent]
public sealed partial class ModularTurretComponent : Component
{
    [DataField]
    public string? MountClass;

    [DataField(required: true)]
    public string Slot = "";
}
