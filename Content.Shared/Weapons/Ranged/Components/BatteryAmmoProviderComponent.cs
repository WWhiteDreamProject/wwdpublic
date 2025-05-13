using Content.Shared.Atmos;

namespace Content.Shared.Weapons.Ranged.Components;

public abstract partial class BatteryAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// How much battery it costs to fire once.
    /// </summary>
    [DataField("fireCost"), ViewVariables(VVAccess.ReadWrite)]
    public float FireCost = 100;

    // WWDP EDIT START
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatFireCost = 25;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatLimit = 450;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool HeatSafety = true;

    // Guess what? Temperature also isn't predicted. I hate the antichrist.
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CurrentTemperature = Atmospherics.T20C;
    // WWDP EDIT END

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!

    [ViewVariables(VVAccess.ReadWrite)]
    public int Shots;

    [ViewVariables(VVAccess.ReadWrite)]
    public int Capacity;
}
