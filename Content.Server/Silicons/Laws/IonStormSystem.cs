using Content.Server.StationEvents.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Silicons.Laws;

public sealed class IonStormSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    // funny
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Threats = "IonStormThreats";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Objects = "IonStormObjects";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Crew = "IonStormCrew";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Adjectives = "IonStormAdjectives";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Verbs = "IonStormVerbs";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string NumberBase = "IonStormNumberBase";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string NumberMod = "IonStormNumberMod";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Areas = "IonStormAreas";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Feelings = "IonStormFeelings";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string FeelingsPlural = "IonStormFeelingsPlural";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Musts = "IonStormMusts";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Requires = "IonStormRequires";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Actions = "IonStormActions";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Allergies = "IonStormAllergies";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string AllergySeverities = "IonStormAllergySeverities";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Concepts = "IonStormConcepts";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Drinks = "IonStormDrinks";
    [ValidatePrototypeId<DatasetPrototype>]
    private const string Foods = "IonStormFoods";

    /// <summary>
    /// Randomly alters the laws of an individual silicon.
    /// </summary>
    public void IonStormTarget(Entity<SiliconLawBoundComponent, IonStormTargetComponent> ent, bool adminlog = true)
    {
        var lawBound = ent.Comp1;
        var target = ent.Comp2;
        if (!_robustRandom.Prob(target.Chance))
            return;

        var laws = _siliconLaw.GetLaws(ent, lawBound);
        if (laws.Laws.Count == 0)
            return;

        // try to swap it out with a random lawset
        if (_robustRandom.Prob(target.RandomLawsetChance))
        {
            var lawsets = _proto.Index<WeightedRandomPrototype>(target.RandomLawsets);
            var lawset = lawsets.Pick(_robustRandom);
            laws = _siliconLaw.GetLawset(lawset);
        }
        // clone it so not modifying stations lawset
        laws = laws.Clone();

        // shuffle them all
        if (_robustRandom.Prob(target.ShuffleChance))
        {
            // hopefully work with existing glitched laws if there are multiple ion storms
            var baseOrder = FixedPoint2.New(1);
            foreach (var law in laws.Laws)
            {
                if (law.Order < baseOrder)
                    baseOrder = law.Order;
            }

            _robustRandom.Shuffle(laws.Laws);

            // change order based on shuffled position
            for (int i = 0; i < laws.Laws.Count; i++)
            {
                laws.Laws[i].Order = baseOrder + i;
            }
        }

        // see if we can remove a random law
        if (laws.Laws.Count > 0 && _robustRandom.Prob(target.RemoveChance))
        {
            var i = _robustRandom.Next(laws.Laws.Count);
            laws.Laws.RemoveAt(i);
        }

        // generate a new law...
        var newLaw = GenerateLaw();

        // see if the law we add will replace a random existing law or be a new glitched order one
        if (laws.Laws.Count > 0 && _robustRandom.Prob(target.ReplaceChance))
        {
            var i = _robustRandom.Next(laws.Laws.Count);
            laws.Laws[i] = new SiliconLaw()
            {
                LawString = newLaw,
                Order = laws.Laws[i].Order
            };
        }
        else
        {
            laws.Laws.Insert(0, new SiliconLaw
            {
                LawString = newLaw,
                Order = -1,
                LawIdentifierOverride = Loc.GetString("ion-storm-law-scrambled-number", ("length", _robustRandom.Next(5, 10)))
            });
        }

        // sets all unobfuscated laws' indentifier in order from highest to lowest priority
        // This could technically override the Obfuscation from the code above, but it seems unlikely enough to basically never happen
        int orderDeduction = -1;

        for (int i = 0; i < laws.Laws.Count; i++)
        {
            var notNullIdentifier = laws.Laws[i].LawIdentifierOverride ?? (i - orderDeduction).ToString();

            if (notNullIdentifier.Any(char.IsSymbol))
            {
                orderDeduction += 1;
            }
            else
            {
                laws.Laws[i].LawIdentifierOverride = (i - orderDeduction).ToString();
            }
        }

        // adminlog is used to prevent adminlog spam.
        if (adminlog)
            _adminLogger.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(ent):silicon} had its laws changed by an ion storm to {laws.LoggingString()}");

        // laws unique to this silicon, dont use station laws anymore
        EnsureComp<SiliconLawProviderComponent>(ent);
        var ev = new IonStormLawsEvent(laws);
        RaiseLocalEvent(ent, ref ev);
    }

    // for your own sake direct your eyes elsewhere
    private string GenerateLaw()
    {
        // pick all values ahead of time to make the logic cleaner
        var threats = Pick(Threats);
        var objects = Pick(Objects);
        var crew1 = Pick(Crew);
        var crew2 = Pick(Crew);
        // WWDP EDIT START
        var adjForms = PickForms(Adjectives);
        var adjective = adjForms[0]; //singular
        var adjectivePlural = Form(adjForms, 1); //plural
        // WWDP EDIT END
        var verb = Pick(Verbs);
        var number = Pick(NumberBase) + " " + Pick(NumberMod);
        var area = Pick(Areas);
        var feeling = Pick(Feelings);
        var feelingPlural = Pick(FeelingsPlural);
        var must = Pick(Musts);
        var require = Pick(Requires);
        var action = Pick(Actions);
        var allergy = Pick(Allergies);
        var allergySeverity = Pick(AllergySeverities);
        var concept = Pick(Concepts);
        var drink = Pick(Drinks);
        var food = Pick(Foods);

        var joined = $"{number} {adjective}";
        // a lot of things have subjects of a threat/crew/object
        var triple = _robustRandom.Next(0, 3) switch
        {
            0 => threats,
            1 => crew1,
            2 => objects,
            _ => throw new IndexOutOfRangeException(),
        };
        var crewAll = _robustRandom.Prob(0.5f) ? crew2 : Loc.GetString("ion-storm-crew");
        var objectsThreats = _robustRandom.Prob(0.5f) ? objects : threats;
        var objectsConcept = _robustRandom.Prob(0.5f) ? objects : concept;
        // s goes ahead of require, is/are
        // i dont think theres a way to do this in fluent
        var (who, plural) = _robustRandom.Next(0, 5) switch
        {
            0 => (Loc.GetString("ion-storm-you"), false),
            1 => (Loc.GetString("ion-storm-the-station"), true),
            2 => (Loc.GetString("ion-storm-the-crew"), true),
            3 => (Loc.GetString("ion-storm-the-job", ("job", crew2)), false),
            _ => (area, true) // THE SINGULARITY REQUIRES THE HAPPY CLOWNS
        };
        var jobChange = _robustRandom.Next(0, 3) switch
        {
            0 => crew1,
            1 => Loc.GetString("ion-storm-clowns"),
            _ => Loc.GetString("ion-storm-heads")
        };
        // WWDP EDIT START
        bool is_part = _robustRandom.Prob(0.5f);
        var part = Loc.GetString("ion-storm-part", ("part", is_part));
        var notpart = Loc.GetString("ion-storm-part" /* but reverse; for better ru loc */, ("notpart", !is_part));
        // WWDP EDIT END
        var harm = _robustRandom.Next(1, 6) switch // WWDP EDIT
        {
            //0 => concept, // Incompatible with the Russian language // WWDP EDIT
            1 => $"{adjective} {threats}",
            2 => $"{adjective} {objects}",
            3 => Loc.GetString("ion-storm-adjective-things", ("adjective", adjectivePlural)), // WWDP EDIT
            4 => crew1,
            _ => Loc.GetString("ion-storm-x-and-y", ("x", crew1), ("y", crew2))
        };

        if (plural) feeling = feelingPlural;

        var subjects = _robustRandom.Prob(0.5f) ? objectsThreats : Loc.GetString("ion-storm-people");

        // message logic!!!
        return _robustRandom.Next(0, 35) switch
        {
            0  => Loc.GetString("ion-storm-law-on-station", ("joined", joined), ("subjects", triple), ("number", number), ("adjective", adjectivePlural)), // WWDP EDIT
            1  => Loc.GetString("ion-storm-law-no-shuttle", ("joined", joined), ("subjects", triple), ("number", number), ("adjective", adjectivePlural)), // WWDP EDIT
            2  => Loc.GetString("ion-storm-law-crew-are", ("who", crewAll), ("joined", joined), ("subjects", objectsThreats), ("number", number), ("adjective", adjectivePlural)), // WWDP EDIT
            3  => Loc.GetString("ion-storm-law-subjects-harmful", ("adjective", adjectivePlural), ("subjects", triple)), // WWDP EDIT
            4  => Loc.GetString("ion-storm-law-must-harmful", ("must", must)),
            5  => Loc.GetString("ion-storm-law-thing-harmful", ("thing", _robustRandom.Prob(0.5f) ? concept : action), ("action", action)), // WWDP EDIT
            6  => Loc.GetString("ion-storm-law-job-harmful", ("adjective", adjectivePlural), ("job", crew1)), // WWDP EDIT
            7  => Loc.GetString("ion-storm-law-having-harmful", ("adjective", adjectivePlural), ("thing", objectsConcept), ("objects", objects)), // WWDP EDIT
            8  => Loc.GetString("ion-storm-law-not-having-harmful", ("adjective", adjectivePlural), ("thing", objectsConcept), ("objects", objects)), // WWDP EDIT
            9  => Loc.GetString("ion-storm-law-requires", ("who", who), ("plural", plural), ("thing", _robustRandom.Prob(0.5f) ? concept : require)), // WWDP EDIT
            10 => Loc.GetString("ion-storm-law-requires-subjects", ("who", who), ("plural", plural), ("joined", joined), ("subjects", triple), ("number", number), ("adjective", adjectivePlural)), // WWDP EDIT
            11 => Loc.GetString("ion-storm-law-allergic", ("who", who), ("plural", plural), ("severity", allergySeverity), ("allergy", _robustRandom.Prob(0.5f) ? concept : allergy)),
            12 => Loc.GetString("ion-storm-law-allergic-subjects", ("who", who), ("plural", plural), ("severity", allergySeverity), ("adjective", adjectivePlural), ("subjects", _robustRandom.Prob(0.5f) ? objects : crew1)), // WWDP EDIT
            13 => Loc.GetString("ion-storm-law-feeling", ("who", who), ("feeling", feeling), ("concept", concept), ("feelingPlural", feelingPlural)), // WWDP EDIT
            14 => Loc.GetString("ion-storm-law-feeling-subjects", ("who", who), ("feeling", feeling), ("joined", joined), ("subjects", triple), ("number", number), ("adjective", adjectivePlural), ("feelingPlural", feelingPlural)), // WWDP EDIT
            15 => Loc.GetString("ion-storm-law-you-are", ("concept", concept)),
            16 => Loc.GetString("ion-storm-law-you-are-subjects", ("joined", joined), ("subjects", triple), ("number", number), ("adjective", adjectivePlural)), // WWDP EDIT
            17 => Loc.GetString("ion-storm-law-you-must-always", ("must", must)),
            18 => Loc.GetString("ion-storm-law-you-must-never", ("must", must)),
            19 => Loc.GetString("ion-storm-law-eat", ("who", crewAll), ("adjective", adjectivePlural), ("food", _robustRandom.Prob(0.5f) ? food : triple)), // WWDP EDIT
            20 => Loc.GetString("ion-storm-law-drink", ("who", crewAll), ("adjective", adjective), ("drink", drink)),
            21 => Loc.GetString("ion-storm-law-change-job", ("who", crewAll), ("adjective", adjectivePlural), ("change", jobChange)), // WWDP EDIT
            22 => Loc.GetString("ion-storm-law-highest-rank", ("who", crew1)),
            23 => Loc.GetString("ion-storm-law-lowest-rank", ("who", crew1)),
            24 => Loc.GetString("ion-storm-law-crew-must", ("who", crewAll), ("must", must)),
            25 => Loc.GetString("ion-storm-law-crew-must-go", ("who", crewAll), ("area", area)),
            26 => Loc.GetString("ion-storm-law-crew-only-1", ("who", crew1), ("part", part), ("notpart", notpart)), // WWDP EDIT
            27 => Loc.GetString("ion-storm-law-crew-only-2", ("who", crew1), ("other", crew2), ("part", part), ("notpart", notpart)), // WWDP EDIT
            28 => Loc.GetString("ion-storm-law-crew-only-subjects", ("adjective", adjectivePlural), ("subjects", subjects), ("part", part), ("notpart", notpart)), // WWDP EDIT
            29 => Loc.GetString("ion-storm-law-crew-must-do", ("must", must), ("part", part), ("notpart", notpart)), // WWDP EDIT
            30 => Loc.GetString("ion-storm-law-crew-must-have", ("adjective", adjectivePlural), ("objects", objects), ("part", part), ("notpart", notpart)), // WWDP EDIT
            31 => Loc.GetString("ion-storm-law-crew-must-eat", ("who", who), ("adjective", adjectivePlural), ("food", food), ("part", part), ("notpart", notpart)), // WWDP EDIT
            32 => Loc.GetString("ion-storm-law-harm", ("who", harm)),
            33 => Loc.GetString("ion-storm-law-protect", ("who", harm)),
            _ => Loc.GetString("ion-storm-law-concept-verb", ("concept", concept), ("verb", verb), ("subjects", triple), ("who", crewAll)) // WWDP EDIT
        };
    }

    /// <summary>
    /// Picks a random value from an ion storm dataset.
    /// All ion storm datasets start with IonStorm.
    /// </summary>
    private string Pick(string name)
    {
        var dataset = _proto.Index<DatasetPrototype>(name);
        return _robustRandom.Pick(dataset.Values);
    }
    // WWDP EDIT START
    /// <summary>
    /// Selects one entry and splits it into forms (singular/plural).
    /// </summary>
    private string[] PickForms(string name)
    {
        var dataset = _proto.Index<DatasetPrototype>(name);
        var raw = _robustRandom.Pick(dataset.Values);
        return raw.Split('|');
    }

    /// <summary>
    /// Safely extracts forms by index.
    /// </summary>
    private static string Form(string[] forms, int index)
    {
        return index < forms.Length ? forms[index] : forms[0];
    }
    // WWDP EDIT END
}
