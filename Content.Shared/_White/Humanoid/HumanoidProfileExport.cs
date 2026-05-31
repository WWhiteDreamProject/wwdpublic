using Content.Shared._White.Preferences;

namespace Content.Shared._White.Humanoid;

/// <summary>
/// This allows character data to be exported to or imported.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExport
{
    /// <summary>
    /// The actual humanoid character profile.
    /// </summary>
    [DataField(required: true)]
    public HumanoidCharacterProfile Profile;

    /// <summary>
    /// The schema version of the exported data.
    /// </summary>
    [DataField]
    public int Version = 1;

    /// <summary>
    /// An identifier for the specific data format version.
    /// </summary>
    [DataField]
    public string ForkId;
}
