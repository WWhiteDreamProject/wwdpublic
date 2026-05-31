using System.Numerics;
using Content.Shared._EE.Contractors.Prototypes;
using Content.Shared._White.Bark;
using Content.Shared._White.CCVar;
using Content.Shared._White.Humanoid.Components;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared._White.Preferences;
using Content.Shared._White.TTS;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Systems;

public sealed class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly GrammarSystem _grammar = default!;

    private ISawmill _sawmill = default!;

    private EntityQuery<GrammarComponent> _grammarQuery;
    private EntityQuery<HumanoidProfileComponent> _humanoidProfileQuery;

    public static readonly Dictionary<Sex,  ProtoId<TTSVoicePrototype>> DefaultSexVoice = new()
    {
        { Sex.Male, "Aidar" },
        { Sex.Female, "Kseniya" },
        { Sex.Unsexed, "Baya" },
    };

    public static int MaxCustomContentLength { get; private set; }
    public static int MaxFlavorLength { get; private set; }
    public static int MaxNameLength { get; private set; }

    public static readonly ProtoId<BarkVoicePrototype> DefaultBark = "Txt1";
    public static readonly ProtoId<BodyTypePrototype> DefaultBodyType = "Normal";
    public static readonly ProtoId<EmployerPrototype> DefaultEmployer = "NanoTrasen";
    public static readonly ProtoId<LifepathPrototype> DefaultLifepath = "Spacer";
    public static readonly ProtoId<NationalityPrototype> DefaultNationality = "Bieselite";
    public static readonly ProtoId<SpeciesPrototype> DefaultSpecies = "Human";
    public static readonly ProtoId<TTSVoicePrototype> DefaultVoice = "Aidar";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("humanoid_profile");

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);

        _grammarQuery = GetEntityQuery<GrammarComponent>();
        _humanoidProfileQuery = GetEntityQuery<HumanoidProfileComponent>();

        _configuration.OnValueChanged(WhiteCVars.MaxCustomContentLength, value => MaxCustomContentLength = value, true);
        _configuration.OnValueChanged(WhiteCVars.MaxFlavorLength, value => MaxFlavorLength = value, true);
        _configuration.OnValueChanged(WhiteCVars.MaxNameLength, value => MaxNameLength = value, true);
    }

    #region Event Handling

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(ent, EntityManager);
        var species = GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Age, ent.Comp.Species);

        args.PushText(Loc.GetString("humanoid-profile-examine", ("age", age), ("species", species), ("user", identity)));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Translates age into a localized, descriptive age category (e.g., "young", "middle-aged").
    /// </summary>
    /// <param name="age">The current humanoid age.</param>
    /// <param name="species">The current humanoid species.</param>
    public string GetAgeRepresentation(int age, ProtoId<SpeciesPrototype> species)
    {
        if (!_prototype.TryIndex(species, out var speciesPrototype))
        {
            _sawmill.Error($"Tried to get age representation of species that couldn't be indexed: {species}");
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }

    /// <summary>
    /// Translates a species prototype ID into a user-friendly, localized display name.
    /// </summary>
    /// <param name="species">The current humanoid species.</param>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (_prototype.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        _sawmill.Error($"Tried to get representation of unknown species: {species}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    /// <summary>
    /// Applies a character profile to a humanoid entity.
    /// </summary>
    /// <param name="ent">The humanoid entity whose character profile should be updated.</param>
    /// <param name="profile">The applied humanoid character profile.</param>
    public void ApplyProfile(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!_humanoidProfileQuery.Resolve(ent, ref ent.Comp))
            return;

        var sexChangedEv = new SexChangedEvent(ent.Comp.Sex, profile.Sex);
        RaiseLocalEvent(ent, ref sexChangedEv);

        ent.Comp.Height = profile.Height;
        ent.Comp.Width = profile.Width;
        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.BodyType = profile.BodyType;
        ent.Comp.Species = profile.Species;
        ent.Comp.Sex = profile.Sex;
        Dirty(ent);

        if (!_grammarQuery.TryComp(ent, out var grammarComp))
            return;

        _grammar.SetGender((ent, grammarComp), profile.Gender);
    }

    /// <summary>
    /// Set the scale of a humanoid entity.
    /// </summary>
    /// <param name="ent">The humanoid entity to set scale.</param>
    /// <param name="scale">The scale to set the entity to</param>
    public void SetScale(Entity<HumanoidProfileComponent?> ent, Vector2 scale)
    {
        if (!_humanoidProfileQuery.Resolve(ent, ref ent.Comp))
            return;

        if (!_prototype.TryIndex(ent.Comp.Species, out var speciesPrototype))
            return;

        ent.Comp.Height = Math.Clamp(scale.Y, speciesPrototype.MinHeight, speciesPrototype.MaxHeight);
        ent.Comp.Width = Math.Clamp(scale.X, speciesPrototype.MinWidth, speciesPrototype.MaxWidth);
        Dirty(ent);
    }

    #endregion
}

/// <summary>
/// Event raised on entity, when its sex is changed.
/// </summary>
[ByRefEvent]
public record struct SexChangedEvent(Sex OldSex, Sex NewSex);
