using Content.Shared._White.Preferences;

namespace Content.Server._White.Preferences;

/// <summary>
/// A server-side container for player preferences data.
/// </summary>
public sealed class PlayerPreferencesData
{
    /// <summary>
    /// Flag indicating whether the player's preferences have been successfully loaded from storage or network.
    /// </summary>
    public bool PreferencesLoaded;

    /// <summary>
    /// Holds the player's preferences, including character profiles and settings.
    /// </summary>
    public PlayerPreferences? Preferences;
}
