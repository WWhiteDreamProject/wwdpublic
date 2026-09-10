namespace Content.Shared._White.Guns.ModularTurret;


[RegisterComponent]
public sealed partial class ModularTurretWeaponComponent : Component
{
    [DataField]
    public string? WeaponClass; 

    [DataField]
    public string? Name;

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
    public List<string>? AcceptedWeaponClasses;

    [DataField(required: true)]
    public string Slot = "";
}
