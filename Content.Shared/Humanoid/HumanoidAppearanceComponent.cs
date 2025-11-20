using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared._White.TTS;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences; // DeltaV
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
public sealed partial class HumanoidAppearanceComponent : Component
{
    public MarkingSet ClientOldMarkings = new();

    [DataField, AutoNetworkedField]
    public MarkingSet MarkingSet = new();

    [DataField, AutoNetworkedField]
    public HashSet<Enum> PermanentlyHidden = new(); // WD EDIT

    // Couldn't these be somewhere else?

    [DataField, AutoNetworkedField]
    public Gender Gender;

    [DataField, AutoNetworkedField]
    public string? DisplayPronouns;

    [DataField, AutoNetworkedField]
    public string? StationAiName;

    [DataField, AutoNetworkedField]
    public string? CyborgName;

    [DataField, AutoNetworkedField]
    public int Age = 18;

    [DataField, AutoNetworkedField]
    public string CustomSpecieName = "";

    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species { get; set; }

    /// <summary>
    ///     The initial profile and base layers to apply to this humanoid.
    /// </summary>
    [DataField]
    public ProtoId<HumanoidProfilePrototype>? Initial { get; private set; }

    /// <summary>
    ///     Skin color of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color SkinColor { get; set; } = Color.FromHex("#C0967F");

    /// <summary>
    ///     Visual layers currently hidden. This will affect the base sprite
    ///     on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<Enum> HiddenLayers = new(); // WD EDIT

    [DataField, AutoNetworkedField]
    public Sex Sex = Sex.Male;

    [DataField, AutoNetworkedField]
    public Color EyeColor = Color.Brown;

    /// <summary>
    ///     Hair color of this humanoid. Used to avoid looping through all markings
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Color? CachedHairColor;

    /// <summary>
    ///     Facial Hair color of this humanoid. Used to avoid looping through all markings
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Color? CachedFacialHairColor;

    /// <summary>
    ///     Which layers of this humanoid that should be hidden on equipping a corresponding item..
    /// </summary>
    [DataField]
    public HashSet<Enum> HideLayersOnEquip = [HumanoidVisualLayers.Hair]; // WD EDIT

    /// <summary>
    /// DeltaV - let paradox anomaly be cloned
    /// </summary>
    [ViewVariables]
    public HumanoidCharacterProfile? LastProfileLoaded;

    /// <summary>
    ///     The height of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Height = 1f;

    /// <summary>
    ///     The width of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Width = 1f;

    // WD EDIT START
    /// <summary>
    ///     Current body type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BodyTypePrototype> BodyType { get; set; } = SharedHumanoidAppearanceSystem.DefaultBodyType;

    [DataField, AutoNetworkedField]
    public ProtoId<TTSVoicePrototype> Voice { get; set; } = SharedHumanoidAppearanceSystem.DefaultVoice;
    // WD EDIT END
}
