using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._EE.Contractors.Prototypes;
using Content.Shared._White.Bark;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Markings.Systems;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared._White.Humanoid.Systems;
using Content.Shared._White.TTS;
using Content.Shared.CCVar;
using Content.Shared.Clothing.Loadouts.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared._White.Preferences;

/// <summary>
/// Character profile. Looks immutable, but uses non-immutable semantics internally for serialization/code sanity.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterProfile
{
    private static readonly Regex RestrictedNameRegex = new(@"[^A-Za-z0-9 '\-]");
    private static readonly Regex ICNameCaseRegex = new(@"^(?<word>\w)|\b(?<word>\w)(?=\w*$)");

    /// <summary>
    /// Dictionary mapping job prototypes to their priority for initial spawn.
    /// </summary>
    [DataField("jobPriorities")]
    private Dictionary<ProtoId<JobPrototype>, JobPriority> _jobPriorities = new()
    {
        {
            SharedGameTicker.FallbackOverflowJob, JobPriority.High
        },
    };

    /// <summary>
    /// Dictionary storing character colors.
    /// </summary>
    [DataField("colors")]
    private Dictionary<ProtoId<BodyColorationPrototype>, Color> _bodyColoration = new()
    {
        {"Skin", Color.White},
        {"Eye", Color.Black},
    };

    /// <summary>
    /// Dictionary storing character markings by marking category.
    /// </summary>
    [DataField("markings")]
    private Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> _markings = new();

    /// <summary>
    /// Dictionary mapping body provider slot IDs to their corresponding entity prototype IDs.
    /// </summary>
    [DataField("bodyProviders")]
    private Dictionary<string, EntProtoId?> _bodyProviders = new();

    /// <summary>
    /// Dictionary storing character loadouts.
    /// </summary>
    [DataField("loadouts")]
    private Dictionary<string, Loadout> _loadouts = new();

    /// <summary>
    /// Set of antag prototype IDs that the player has opted into.
    /// </summary>
    [DataField("antagPreferences")]
    private HashSet<ProtoId<AntagPrototype>> _antagPreferences = new();

    /// <summary>
    /// Set of trait prototype IDs that are enabled for this character.
    /// </summary>
    [DataField("traitPreferences")]
    private HashSet<ProtoId<TraitPrototype>> _traitPreferences = new();

    /// <summary>
    /// Data for applying bark percentage settings.
    /// </summary>
    [DataField]
    public BarkPercentageApplyData BarkSettings { get; set; } = BarkPercentageApplyData.Default;

    /// <summary>
    /// The character's height.
    /// </summary>
    [DataField]
    public float Height { get; private set; }

    /// <summary>
    /// The character's width.
    /// </summary>
    [DataField]
    public float Width { get; private set; }

    /// <summary>
    /// The character's gender.
    /// </summary>
    [DataField]
    public Gender Gender { get; private set; } = Gender.Male;

    /// <summary>
    /// The character's age.
    /// </summary>
    [DataField]
    public int Age { get; set; } = 18;

    /// <inheritdoc cref="_jobPriorities"/>
    public IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities => _jobPriorities;

    /// <inheritdoc cref="_bodyColoration"/>
    public IReadOnlyDictionary<ProtoId<BodyColorationPrototype>, Color> BodyColoration => _bodyColoration;

    /// <inheritdoc cref="_markings"/>
    public IReadOnlyDictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings => _markings;

    /// <inheritdoc cref="_bodyProviders"/>
    public IReadOnlyDictionary<string, EntProtoId?> BodyProviders => _bodyProviders;

    /// <inheritdoc cref="_loadouts"/>
    public IReadOnlyDictionary<string, Loadout> Loadouts => _loadouts;

    /// <inheritdoc cref="_antagPreferences"/>
    public IReadOnlySet<ProtoId<AntagPrototype>> AntagPreferences => _antagPreferences;

    /// <inheritdoc cref="_traitPreferences"/>
    public IReadOnlySet<ProtoId<TraitPrototype>> TraitPreferences => _traitPreferences;

    /// <summary>
    /// Determines the behavior when a preferred job is unavailable.
    /// </summary>
    [DataField]
    public PreferenceUnavailableMode PreferenceUnavailable { get; private set; } = PreferenceUnavailableMode.SpawnAsOverflow;

    /// <summary>
    /// The preferred bark voice for the character.
    /// </summary>
    [DataField]
    public ProtoId<BarkVoicePrototype> Bark { get; set; } = HumanoidProfileSystem.DefaultBark;

    /// <summary>
    /// The character's body type.
    /// </summary>
    [DataField]
    public ProtoId<BodyTypePrototype> BodyType { get; private set; } = HumanoidProfileSystem.DefaultBodyType;

    /// <summary>
    /// The character's employer.
    /// </summary>
    [DataField]
    public ProtoId<EmployerPrototype> Employer { get; private set; } = HumanoidProfileSystem.DefaultEmployer;

    /// <summary>
    /// The character's lifepath.
    /// </summary>
    [DataField]
    public ProtoId<LifepathPrototype> Lifepath { get; private set; } = HumanoidProfileSystem.DefaultLifepath;

    /// <summary>
    /// The character's nationality.
    /// </summary>
    [DataField]
    public ProtoId<NationalityPrototype> Nationality { get; private set; } = HumanoidProfileSystem.DefaultNationality;

    /// <summary>
    /// The character's species.
    /// </summary>
    [DataField]
    public ProtoId<SpeciesPrototype> Species { get; private set; } = HumanoidProfileSystem.DefaultSpecies;

    /// <summary>
    /// The character's voice.
    /// </summary>
    [DataField]
    public ProtoId<TTSVoicePrototype> Voice { get; set; } = HumanoidProfileSystem.DefaultVoice;

    /// <summary>
    /// The character's sex.
    /// </summary>
    [DataField]
    public Sex Sex { get; private set; } = Sex.Male;

    /// <summary>
    /// The preferred spot to spawn into a round.
    /// </summary>
    [DataField]
    public SpawnPriority SpawnPriority { get; private set; } = SpawnPriority.None;

    /// <summary>
    /// Detailed text that can appear for the character if <see cref="CCVars.FlavorText"/> is enabled.
    /// </summary>
    [DataField]
    public string Flavor { get; private set; } = string.Empty;

    /// <summary>
    /// The character's name.
    /// </summary>
    [DataField]
    public string Name { get; private set; } = "John Doe";

    [ViewVariables]
    public string Summary =>
        Loc.GetString(
            "humanoid-character-profile-summary",
            ("name", Name),
            ("gender", Gender.ToString().ToLowerInvariant()),
            ("age", Age)
        );

    /// <summary>
    /// Initializes a new instance with specified properties.
    /// </summary>
    public HumanoidCharacterProfile(
        Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities,
        Dictionary<ProtoId<BodyColorationPrototype>, Color> bodyColoration,
        Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markings,
        Dictionary<string, EntProtoId?> bodyProviders,
        Dictionary<string, Loadout> loadouts,
        HashSet<ProtoId<AntagPrototype>> antagPreferences,
        HashSet<ProtoId<TraitPrototype>> traitPreferences,
        BarkPercentageApplyData barkSettings,
        float height,
        float width,
        Gender gender,
        int age,
        PreferenceUnavailableMode preferenceUnavailable,
        ProtoId<BarkVoicePrototype> bark,
        ProtoId<BodyTypePrototype> bodyType,
        ProtoId<EmployerPrototype> employer,
        ProtoId<LifepathPrototype> lifepath,
        ProtoId<NationalityPrototype> nationality,
        ProtoId<SpeciesPrototype> species,
        ProtoId<TTSVoicePrototype> voice,
        Sex sex,
        SpawnPriority spawnPriority,
        string flavor,
        string name
        )
    {
        _jobPriorities = jobPriorities;
        _bodyColoration = bodyColoration;
        _markings = markings;
        _bodyProviders = bodyProviders;
        _loadouts = loadouts;
        _antagPreferences = antagPreferences;
        _traitPreferences = traitPreferences;
        BarkSettings = barkSettings;
        Height = height;
        Width = width;
        Gender = gender;
        Age = age;
        PreferenceUnavailable = preferenceUnavailable;
        Bark = bark;
        BodyType = bodyType;
        Employer = employer;
        Lifepath = lifepath;
        Nationality = nationality;
        Species = species;
        Voice = voice;
        Sex = sex;
        SpawnPriority = spawnPriority;
        Flavor = flavor;
        Name = name;

        EnsureValid();
    }

    /// <summary>
    /// Initializes a new instance by copying another profile.
    /// </summary>
    /// <param name="other">The <see cref="HumanoidCharacterProfile"/> to copy.</param>
    public HumanoidCharacterProfile(HumanoidCharacterProfile other)
    {
        _jobPriorities = new(other.JobPriorities);
        _bodyColoration = new(other.BodyColoration);
        _markings = new(other.Markings);
        _bodyProviders = new(other.BodyProviders);
        _loadouts = new(other.Loadouts);
        _antagPreferences = new(other.AntagPreferences);
        _traitPreferences = new(other.TraitPreferences);
        BarkSettings = other.BarkSettings;
        Height = other.Height;
        Width = other.Width;
        Gender = other.Gender;
        Age = other.Age;
        PreferenceUnavailable = other.PreferenceUnavailable;
        Bark = other.Bark;
        BodyType = other.BodyType;
        Employer = other.Employer;
        Lifepath = other.Lifepath;
        Nationality = other.Nationality;
        Species = other.Species;
        Voice = other.Voice;
        Sex = other.Sex;
        SpawnPriority = other.SpawnPriority;
        Flavor = other.Flavor;
        Name = other.Name;
    }

    /// <summary>
    /// Initializes a new instance with default values.
    /// </summary>
    public HumanoidCharacterProfile() { }

    public override bool Equals(object? obj)
    {
        return obj is HumanoidCharacterProfile other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(_jobPriorities);
        hashCode.Add(_bodyColoration);
        hashCode.Add(_markings);
        hashCode.Add(_bodyProviders);
        hashCode.Add(_loadouts);
        hashCode.Add(_antagPreferences);
        hashCode.Add(_traitPreferences);
        hashCode.Add(BarkSettings);
        hashCode.Add(Height);
        hashCode.Add(Width);
        hashCode.Add(Gender);
        hashCode.Add(Age);
        hashCode.Add(PreferenceUnavailable);
        hashCode.Add(Bark);
        hashCode.Add(BodyType);
        hashCode.Add(Employer);
        hashCode.Add(Lifepath);
        hashCode.Add(Nationality);
        hashCode.Add(Species);
        hashCode.Add(Voice);
        hashCode.Add(Sex);
        hashCode.Add(SpawnPriority);
        hashCode.Add(Flavor);
        hashCode.Add(Name);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(HumanoidCharacterProfile? left, HumanoidCharacterProfile? right)
    {
        if (left == null)
            return right == null;

        if (right == null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(HumanoidCharacterProfile? left, HumanoidCharacterProfile? right)
    {
        return !(left == right);
    }

    public bool Equals(HumanoidCharacterProfile other)
    {
        if (!_jobPriorities.SequenceEqual(other._jobPriorities))
            return false;

        if (!_bodyColoration.SequenceEqual(other._bodyColoration))
            return false;

        if (!_markings.SequenceEqual(other._markings))
            return false;

        if (!_bodyProviders.SequenceEqual(other._bodyProviders))
            return false;

        if (!_loadouts.SequenceEqual(other._loadouts))
            return false;

        if (!_antagPreferences.SequenceEqual(other._antagPreferences))
            return false;

        if (!_traitPreferences.SequenceEqual(other._traitPreferences))
            return false;

        if (BarkSettings != other.BarkSettings)
            return false;

        if (Height != other.Height)
            return false;

        if (Width != other.Width)
            return false;

        if (Gender != other.Gender)
            return false;

        if (Age != other.Age)
            return false;

        if (PreferenceUnavailable != other.PreferenceUnavailable)
            return false;

        if (Bark != other.Bark)
            return false;

        if (BodyType != other.BodyType)
            return false;

        if (Employer != other.Employer)
            return false;

        if (Lifepath != other.Lifepath)
            return false;

        if (Nationality != other.Nationality)
            return false;

        if (Species != other.Species)
            return false;

        if (Voice != other.Voice)
            return false;

        if (Sex != other.Sex)
            return false;

        if (SpawnPriority != other.SpawnPriority)
            return false;

        if (Flavor != other.Flavor)
            return false;

        if (Name != other.Name)
            return false;

        return true;
    }

    public DataNode ToDataNode(ISerializationManager? serialization = null, IConfigurationManager? configuration = null)
    {
        IoCManager.Resolve(ref serialization);
        IoCManager.Resolve(ref configuration);

        var export = new HumanoidProfileExport()
        {
            ForkId = configuration.GetCVar(CVars.BuildForkId),
            Profile = this,
        };

        var dataNode = serialization.WriteValue(export, alwaysWrite: true, notNullableOverride: true);
        return dataNode;
    }

    /// <summary>
    /// Returns a new charter with new age.
    /// </summary>
    /// <param name="age">The age to use for the profile.</param>
    /// <returns>A new charter with the specified age.</returns>
    public HumanoidCharacterProfile WithAge(int age)
    {
        return new(this) { Age = ValidateAge(age) };
    }

    /// <summary>
    /// Returns a new charter with new antag preference.
    /// </summary>
    /// <param name="antag">The antag whose preference needs to be changed.</param>
    /// <param name="preference">The antag preference to use for the profile.</param>
    /// <returns>A new charter with the specified antag preference.</returns>
    public HumanoidCharacterProfile WithAntagPreference(ProtoId<AntagPrototype> antag, bool preference)
    {
        var antagPreferences = new HashSet<ProtoId<AntagPrototype>>(_antagPreferences);
        if (preference)
        {
            antagPreferences.Add(antag);
        }
        else
        {
            antagPreferences.Remove(antag);
        }

        return new(this) { _antagPreferences = ValidateAntagPreferences(antagPreferences), };
    }

    /// <summary>
    /// Returns a new charter with new antag preferences.
    /// </summary>
    /// <param name="antagPreferences">The antag preferences to use for the profile.</param>
    /// <returns>A new charter with the specified antag preferences.</returns>
    public HumanoidCharacterProfile WithAntagPreferences(HashSet<ProtoId<AntagPrototype>> antagPreferences)
    {
        return new(this) { _antagPreferences = ValidateAntagPreferences(antagPreferences) };
    }

    /// <summary>
    /// Returns a new charter with new bark.
    /// </summary>
    /// <param name="bark">The bark to use for the profile.</param>
    /// <returns>A new charter with the specified bark.</returns>
    public HumanoidCharacterProfile WithBark(ProtoId<BarkVoicePrototype> bark)
    {
        return new(this) { Bark = bark };
    }

    /// <summary>
    /// Returns a new charter with new bark settings.
    /// </summary>
    /// <param name="barkSettings">The bark settings to use for the profile.</param>
    /// <returns>A new charter with the specified bark settings.</returns>
    public HumanoidCharacterProfile WithBarkSettings(BarkPercentageApplyData barkSettings)
    {
        return new(this) { BarkSettings = barkSettings };
    }

    /// <summary>
    /// Returns a new charter with new body type.
    /// </summary>
    /// <param name="bodyType">The body type to use for the profile.</param>
    /// <returns>A new charter with the specified body type.</returns>
    public HumanoidCharacterProfile WithBodyType(ProtoId<BodyTypePrototype> bodyType)
    {
        return new(this) { BodyType = ValidateBodyType(bodyType) };
    }

    /// <summary>
    /// Returns a new charter with new color.
    /// </summary>
    /// <param name="coloration">The body coloration whose color needs to be changed.</param>
    /// <param name="color">The color to use for the profile.</param>
    /// <returns>A new charter with the specified color.</returns>
    public HumanoidCharacterProfile WithColor(ProtoId<BodyColorationPrototype> coloration, Color color)
    {
        var bodyColoration = _bodyColoration.ShallowClone();
        bodyColoration[coloration] = color;

        return new(this) { _bodyColoration = ValidateBodyColoration(bodyColoration) };
    }

    /// <summary>
    /// Returns a new charter with new employer.
    /// </summary>
    /// <param name="employer">The employer to use for the profile.</param>
    /// <returns>A new charter with the specified employer.</returns>
    public HumanoidCharacterProfile WithEmployer(ProtoId<EmployerPrototype> employer)
    {
        return new(this) { Employer = employer };
    }

    /// <summary>
    /// Returns a new charter with new flavor.
    /// </summary>
    /// <param name="flavor">The flavor to use for the profile.</param>
    /// <returns>A new charter with the specified flavor.</returns>
    public HumanoidCharacterProfile WithFlavor(string flavor)
    {
        return new(this) { Flavor = ValidateFlavor(flavor) };
    }

    /// <summary>
    /// Returns a new charter with new gender.
    /// </summary>
    /// <param name="gender">The gender to use for the profile.</param>
    /// <returns>A new charter with the specified gender.</returns>
    public HumanoidCharacterProfile WithGender(Gender gender)
    {
        return new(this) { Gender = gender };
    }

    /// <summary>
    /// Returns a new charter with new height.
    /// </summary>
    /// <param name="height">The height to use for the profile.</param>
    /// <returns>A new charter with the specified height.</returns>
    public HumanoidCharacterProfile WithHeight(float height)
    {
        return new(this) { Height = height };
    }

    /// <summary>
    /// Returns a new charter with new job priority.
    /// </summary>
    /// <param name="job">The job whose priority needs to be changed.</param>
    /// <param name="priority">The job priority to use for the profile.</param>
    /// <returns>A new charter with the specified job priority.</returns>
    public HumanoidCharacterProfile WithJobPriority(ProtoId<JobPrototype> job, JobPriority priority)
    {
        var jobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(_jobPriorities);
        if (priority == JobPriority.Never)
        {
            jobPriorities.Remove(job);
        }
        else if (priority == JobPriority.High)
        {
            foreach (var (jobId, value) in jobPriorities)
            {
                if (value == JobPriority.High)
                    jobPriorities[jobId] = JobPriority.Medium;
            }

            jobPriorities[job] = priority;
        }
        else
        {
            jobPriorities[job] = priority;
        }

        return new(this) { _jobPriorities = ValidateJobPriorities(jobPriorities), };
    }

    /// <summary>
    /// Returns a new charter with new job priorities.
    /// </summary>
    /// <param name="jobPriorities">A dictionary of job prototypes and their desired priorities.</param>
    /// <returns>A new charter with the specified job priorities.</returns>
    public HumanoidCharacterProfile WithJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
    {
        return new(this) { _jobPriorities = ValidateJobPriorities(jobPriorities), };
    }

    /// <summary>
    /// Returns a new charter with new lifepath.
    /// </summary>
    /// <param name="lifepath">The lifepath to use for the profile.</param>
    /// <returns>A new charter with the specified lifepath.</returns>
    public HumanoidCharacterProfile WithLifepath(ProtoId<LifepathPrototype> lifepath)
    {
        return new(this) { Lifepath = lifepath };
    }

    /// <summary>
    /// Returns a new charter with new loadouts.
    /// </summary>
    /// <param name="loadouts">The loadouts to use for the profile.</param>
    /// <returns>A new charter with the specified loadouts.</returns>
    public HumanoidCharacterProfile WithLoadout(Dictionary<string, Loadout> loadouts)
    {
        return new(this) { _loadouts = loadouts };
    }

    /// <summary>
    /// Returns a new charter with new markings.
    /// </summary>
    /// <param name="markings">The markings to use for the profile.</param>
    /// <returns>A new charter with the specified markings.</returns>
    public HumanoidCharacterProfile WithMarkings(Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markings)
    {
        return new(this) { _markings = ValidateMarkings(markings), };
    }

    /// <summary>
    /// Returns a new charter with new name.
    /// </summary>
    /// <param name="name">The name to use for the profile.</param>
    /// <returns>A new charter with the specified name.</returns>
    public HumanoidCharacterProfile WithName(string name)
    {
        return new(this) { Name = ValidateName(name) };
    }

    /// <summary>
    /// Returns a new charter with new nationality.
    /// </summary>
    /// <param name="nationality">The nationality to use for the profile.</param>
    /// <returns>A new charter with the specified nationality.</returns>
    public HumanoidCharacterProfile WithNationality(ProtoId<NationalityPrototype> nationality)
    {
        return new(this) { Nationality = nationality };
    }

    /// <summary>
    /// Returns a new charter with new preference unavailable.
    /// </summary>
    /// <param name="preferenceUnavailable">The preference unavailable to use for the profile.</param>
    /// <returns>A new charter with the specified preference unavailable.</returns>
    public HumanoidCharacterProfile WithPreferenceUnavailable(PreferenceUnavailableMode preferenceUnavailable)
    {
        return new(this) { PreferenceUnavailable = preferenceUnavailable };
    }

    /// <summary>
    /// Returns a new charter with new sex.
    /// </summary>
    /// <param name="sex">The sex to use for the profile.</param>
    /// <returns>A new charter with the specified sex.</returns>
    public HumanoidCharacterProfile WithSex(Sex sex)
    {
        var charter = new HumanoidCharacterProfile(this);

        charter.Sex = charter.ValidateSex(sex);
        charter.Voice = charter.ValidateVoice(Voice);

        charter._markings = charter.ValidateMarkings(_markings);

        return charter;
    }

    /// <summary>
    /// Returns a new charter with new spawn priority.
    /// </summary>
    /// <param name="spawnPriority">The spawn priority to use for the profile.</param>
    /// <returns>A new charter with the specified spawn priority.</returns>
    public HumanoidCharacterProfile WithSpawnPriority(SpawnPriority spawnPriority)
    {
        return new(this) { SpawnPriority = spawnPriority };
    }

    /// <summary>
    /// Returns a new charter with new species.
    /// </summary>
    /// <param name="species">The species to use for the profile.</param>
    /// <returns>A new charter with the specified species.</returns>
    public HumanoidCharacterProfile WithSpecies(ProtoId<SpeciesPrototype> species)
    {
        var charter = new HumanoidCharacterProfile(this);

        charter.Species = charter.ValidateSpecies(species);
        charter.Sex = charter.ValidateSex(Sex);

        charter.Age = charter.ValidateAge(Age);
        charter.BodyType = charter.ValidateBodyType(BodyType);
        charter.Voice = charter.ValidateVoice(Voice);
        charter._bodyColoration = charter.ValidateBodyColoration(_bodyColoration);
        charter._markings = charter.ValidateMarkings(_markings);

        return charter;
    }

    /// <summary>
    /// Returns a new charter with new trait preference.
    /// </summary>
    /// <param name="trait">The trait whose preference needs to be changed.</param>
    /// <param name="preference">The trait preference to use for the profile.</param>
    /// <returns>A new charter with the specified trait preference.</returns>
    public HumanoidCharacterProfile WithTraitPreference(ProtoId<TraitPrototype> trait, bool preference)
    {
        var traitPreferences = new HashSet<ProtoId<TraitPrototype>>(_traitPreferences);
        if (preference)
        {
            traitPreferences.Add(trait);
        }
        else
        {
            traitPreferences.Remove(trait);
        }

        return new(this) { _traitPreferences = ValidateTraitPreferences(traitPreferences) };
    }

    /// <summary>
    /// Returns a new charter with new trait preferences.
    /// </summary>
    /// <param name="traitPreferences">The trait preferences to use for the profile.</param>
    /// <returns>A new charter with the specified trait preferences.</returns>
    public HumanoidCharacterProfile WithTraitPreferences(HashSet<ProtoId<TraitPrototype>> traitPreferences)
    {
        return new(this) { _traitPreferences = ValidateTraitPreferences(traitPreferences) };
    }

    /// <summary>
    /// Returns a new charter with new voice.
    /// </summary>
    /// <param name="voice">The voice to use for the profile.</param>
    /// <returns>A new charter with the specified voice.</returns>
    public HumanoidCharacterProfile WithVoice(ProtoId<TTSVoicePrototype> voice)
    {
        return new(this) { Voice = ValidateVoice(voice) };
    }

    /// <summary>
    /// Returns a new charter with new width.
    /// </summary>
    /// <param name="width">The width to use for the profile.</param>
    /// <returns>A new charter with the specified width.</returns>
    public HumanoidCharacterProfile WithWidth(float width)
    {
        return new(this) { Width = width };
    }

    public static HumanoidCharacterProfile FromStream(Stream stream, ISerializationManager? serialization = null)
    {
        IoCManager.Resolve(ref serialization);

        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        var root = yamlStream.Documents[0].RootNode;
        HumanoidCharacterProfile profile;
        if (root["version"].Equals(new YamlScalarNode("1")))
        {
            var export = serialization.Read<HumanoidProfileExport>(root.ToDataNode(), notNullableOverride: true);
            profile = export.Profile;
        }
        else
        {
            throw new InvalidOperationException($"Unknown version {root["version"]}");
        }

        profile.EnsureValid();
        return profile;
    }

    public static HumanoidCharacterProfile Random(HashSet<ProtoId<SpeciesPrototype>>? ignoredSpecies = null)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        var possibleSpecies = new List<ProtoId<SpeciesPrototype>>();
        foreach (var species in prototypeManager.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (!species.SetPreference)
                continue;

            if (ignoredSpecies != null && ignoredSpecies.Contains(species))
                continue;

            possibleSpecies.Add(species);
        }

        if (possibleSpecies.Count == 0)
            return new();

        return Random(random.Pick(possibleSpecies));
    }

    public static HumanoidCharacterProfile Random(ProtoId<SpeciesPrototype> species)
    {
        var charter = new HumanoidCharacterProfile();

        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        var namingSystem = entitySystemManager.GetEntitySystem<NamingSystem>();

        var speciesPrototype = prototypeManager.Index(species);

        charter.Age = random.Next(speciesPrototype.MinAge, speciesPrototype.OldAge);
        charter.Sex = random.Pick(speciesPrototype.Sexes);
        charter.Species = species;

        switch (charter.Sex)
        {
            case Sex.Male:
                charter.Gender = Gender.Male;
                break;
            case Sex.Female:
                charter.Gender = Gender.Female;
                break;
        }

        charter.Name = namingSystem.GenerateName(speciesPrototype.Naming, charter.Gender);

        return charter;
    }

    /// <summary>
    /// Ensures that all aspects of the character profile are valid and conform to game rules.
    /// </summary>
    public void EnsureValid()
    {
        Species = ValidateSpecies(Species);
        Sex = ValidateSex(Sex);

        Age = ValidateAge(Age);
        BodyType = ValidateBodyType(BodyType);
        Flavor = ValidateFlavor(Flavor);
        Name = ValidateName(Name);
        Voice = ValidateVoice(Voice);

        _antagPreferences = ValidateAntagPreferences(_antagPreferences);
        _bodyProviders = ValidateBodyProviders(_bodyProviders);
        _bodyColoration = ValidateBodyColoration(_bodyColoration);
        _jobPriorities = ValidateJobPriorities(_jobPriorities);
        _markings = ValidateMarkings(_markings);
        _traitPreferences = ValidateTraitPreferences(_traitPreferences);

        // TODO loadouts, bark, height and width validate.
    }

    /// <summary>
    /// Ensures that all aspects of the character profile are valid and conform to game rules.
    /// </summary>
    public HumanoidCharacterProfile Validated()
    {
        var charter = new HumanoidCharacterProfile(this);
        charter.EnsureValid();
        return charter;
    }

    public HumanoidCharacterProfile Clone()
    {
        return new (this);
    }

    private Dictionary<ProtoId<JobPrototype>, JobPriority> ValidateJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var validJobPriorities = jobPriorities.ShallowClone();
        var hasHighPriority = false;
        foreach (var (job, priority) in validJobPriorities)
        {
            if (!prototypeManager.TryIndex(job, out var jobPrototype) || jobPrototype.SetPreference)
            {
                validJobPriorities.Remove(job);
                continue;
            }

            if (priority == JobPriority.Never)
                validJobPriorities.Remove(job);

            if (priority != JobPriority.High)
                continue;

            if (hasHighPriority)
                validJobPriorities[job] = JobPriority.Medium;

            hasHighPriority = true;
        }

        return validJobPriorities;
    }

    private Dictionary<ProtoId<BodyColorationPrototype>, Color> ValidateBodyColoration(Dictionary<ProtoId<BodyColorationPrototype>, Color> colors)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var speciesPrototype = prototypeManager.Index(Species);

        var validColors = colors.ShallowClone();
        foreach (var (bodyColoration, color) in colors)
        {
            if (!speciesPrototype.Coloration.TryGetValue(bodyColoration, out var coloration))
                continue;

            var colorationStrategy = prototypeManager.Index(coloration).Strategy;
            validColors[bodyColoration] = colorationStrategy.EnsureVerified(color);
        }

        return validColors;
    }

    private Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> ValidateMarkings(Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markingsSet)
    {
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        var componentFactory = IoCManager.Resolve<IComponentFactory>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var bodySystem = entitySystemManager.GetEntitySystem<SharedBodySystem>();
        var markingsSystem = entitySystemManager.GetEntitySystem<SharedMarkingsSystem>();

        var speciesPrototype = prototypeManager.Index(Species);
        var dollPrototype = prototypeManager.Index(speciesPrototype.DollPrototype);

        if (!dollPrototype.TryGetComponent<BodyComponent>(out var body, componentFactory))
            return new();

        var bodyProviders = new List<EntProtoId>();
        foreach (var bodyProvider in BodyProviders.Values)
        {
            if (!bodyProvider.HasValue)
                continue;

            bodyProviders.Add(bodyProvider.Value);
        }

        foreach (var (id, bodyProviderSlot) in bodySystem.GetProviderSlots(body))
        {
            if (BodyProviders.ContainsKey(id))
                continue;

            if (bodyProviderSlot.StartingProvider is not {} startingProvider)
                continue;

            bodyProviders.Add(startingProvider);
        }

        var validMarkings = markingsSet.ShallowClone();

        foreach (var bodyProvider in bodyProviders)
        {
            if (!markingsSystem.TryGetData(bodyProvider, out var markingData))
                continue;

            var markings = markingsSet.GetValueOrDefault(markingData.Value.Category)?.ShallowClone() ?? [];

            markingManager.EnsureValidColors(markings, markingData.Value.Group, _bodyColoration);
            markingManager.EnsureValidGroupAndSex(markings, markingData.Value.Group, Sex);
            markingManager.EnsureValidLimits(markings, markingData.Value.Group, _bodyColoration);

            validMarkings[markingData.Value.Category] = markings;
        }

        return validMarkings;
    }

    private Dictionary<string, EntProtoId?> ValidateBodyProviders(Dictionary<string, EntProtoId?> bodyProviders)
    {
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        var componentFactory = IoCManager.Resolve<IComponentFactory>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var bodySystem = entitySystemManager.GetEntitySystem<SharedBodySystem>();

        var speciesPrototype = prototypeManager.Index(Species);
        var dollPrototype = prototypeManager.Index(speciesPrototype.DollPrototype);

        if (!dollPrototype.TryGetComponent<BodyComponent>(out var body, componentFactory))
            return new();

        var bodyProviderSlots = bodySystem.GetProviderSlots(body);

        var validBodyProviders = bodyProviders.ShallowClone();
        foreach (var (id, provider) in validBodyProviders)
        {
            if (bodyProviderSlots.ContainsKey(id) && prototypeManager.HasIndex(provider))
                continue;

            validBodyProviders.Remove(id);
        }

        return validBodyProviders;
    }

    private HashSet<ProtoId<AntagPrototype>> ValidateAntagPreferences(HashSet<ProtoId<AntagPrototype>> antagPreferences)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var validAntagPreferences = new HashSet<ProtoId<AntagPrototype>>(antagPreferences);
        foreach (var antag in validAntagPreferences)
        {
            if (prototypeManager.TryIndex(antag, out var antagPrototype) && antagPrototype.SetPreference)
                continue;

            validAntagPreferences.Remove(antag);
        }

        return validAntagPreferences;
    }

    private HashSet<ProtoId<TraitPrototype>> ValidateTraitPreferences(HashSet<ProtoId<TraitPrototype>> traitPreferences)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var validTraitPreferences = new HashSet<ProtoId<TraitPrototype>>(traitPreferences);
        foreach (var trait in validTraitPreferences)
        {
            if (prototypeManager.TryIndex(trait, out var traitPrototype) && traitPrototype.SetPreference)
                continue;

            validTraitPreferences.Remove(trait);
        }

        return validTraitPreferences;
    }

    private int ValidateAge(int age)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var speciesPrototype = prototypeManager.Index(Species);

        return Math.Clamp(age, speciesPrototype.MinAge, speciesPrototype.MaxAge);
    }

    private ProtoId<BodyTypePrototype> ValidateBodyType(ProtoId<BodyTypePrototype> bodyType)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var speciesPrototype = prototypeManager.Index(Species);

        if (speciesPrototype.BodyTypes.Contains(bodyType))
            return bodyType;

        return speciesPrototype.BodyTypes.FirstOrDefault();
    }

    private ProtoId<SpeciesPrototype> ValidateSpecies(ProtoId<SpeciesPrototype> species)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var speciesPrototype = prototypeManager.Index(species);

        if (!speciesPrototype.SetPreference)
            return HumanoidProfileSystem.DefaultSpecies;

        return species;
    }

    private ProtoId<TTSVoicePrototype> ValidateVoice(ProtoId<TTSVoicePrototype> voice)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var voicePrototype = prototypeManager.Index(voice);
        if (voicePrototype.Sex == Sex.Unsexed)
            return voice;

        if (Sex == voicePrototype.Sex)
            return voice;

        return HumanoidProfileSystem.DefaultSexVoice[Sex];
    }

    private Sex ValidateSex(Sex sex)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var speciesPrototype = prototypeManager.Index(Species);

        if (speciesPrototype.Sexes.Contains(sex))
            return sex;

        return speciesPrototype.Sexes.FirstOrDefault();
    }

    private string ValidateFlavor(string flavor)
    {
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();

        flavor = FormattedMessage.RemoveMarkupOrThrow(flavor);
        if (flavor.Length > HumanoidProfileSystem.MaxFlavorLength)
            flavor = Flavor[..HumanoidProfileSystem.MaxFlavorLength];

        return flavor;
    }

    private string ValidateName(string name)
    {
        var configurationManager = IoCManager.Resolve<IConfigurationManager>();
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var namingSystem = entitySystemManager.GetEntitySystem<NamingSystem>();

        var speciesPrototype = prototypeManager.Index(Species);

        name = name.Trim();

        if (configurationManager.GetCVar(CCVars.RestrictedNames))
            name = RestrictedNameRegex.Replace(name, string.Empty);

        if (configurationManager.GetCVar(CCVars.ICNameCase))
            name = ICNameCaseRegex.Replace(name, m => m.Groups["word"].Value.ToUpper());

        if (name.Length > HumanoidProfileSystem.MaxNameLength)
            name = name[..HumanoidProfileSystem.MaxNameLength];

        if (string.IsNullOrEmpty(name))
            name = namingSystem.GenerateName(speciesPrototype.Naming, Gender);

        return name;
    }
}

/// <summary>
/// The spawn priority preference for a profile.
/// </summary>
public enum SpawnPriority
{
    None = 0,
    Arrivals = 1,
    Cryosleep = 2,
}
