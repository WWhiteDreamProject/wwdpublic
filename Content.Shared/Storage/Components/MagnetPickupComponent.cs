using Content.Shared.Inventory;

namespace Content.Server.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class MagnetPickupComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    [AutoPausedField]
    public TimeSpan NextScan = TimeSpan.Zero;

    /// <summary>
    /// If true, ignores SlotFlags and can magnet pickup on hands/ground.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ForcePickup = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1f;
}
