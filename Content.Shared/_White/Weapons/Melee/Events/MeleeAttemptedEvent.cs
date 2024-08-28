namespace Content.Shared._White.Weapons.Melee.Events;

/// <summary>
/// Raised on a melee when someone is attempting to attack with it.
/// Cancel this event to prevent it from attacking.
/// </summary>
[ByRefEvent]
public record struct MeleeAttemptedEvent
{
    /// <summary>
    /// The user attempting to attack with the weapon.
    /// </summary>
    public EntityUid User;

    /// <summary>
    /// The weapon being used.
    /// </summary>
    public EntityUid Used;

    public bool Cancelled { get; private set; }

    /// </summary>
    /// Prevent the weapon from attacking
    /// </summary>
    public void Cancel()
    {
        Cancelled = true;
    }

    /// </summary>
    /// Allow the weapon to attack again, only use if you know what you are doing
    /// </summary>
    public void Uncancel()
    {
        Cancelled = false;
    }
}
