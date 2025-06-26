namespace Content.Shared.Preferences
{
    /// <summary>
    /// Preferred uplink type for the character. Stored in the database!
    /// </summary>
    public enum UplinkPreference
    {
        None = 0,
        PDA = 1,
        Implant = 2,
        Radio = 3,
        TeleCrystals = 4
    }
}
