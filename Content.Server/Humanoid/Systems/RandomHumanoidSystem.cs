using System.Linq;
using Content.Server.Humanoid.Components;
using Content.Server.RandomMetadata;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Humanoid.Systems;

/// <summary>
///     This deals with spawning and setting up random humanoids.
/// </summary>
public sealed class RandomHumanoidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    private HashSet<string> _notRoundStartSpecies = new();
    private HashSet<string> _allSpecies = new(); // WWDP

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomHumanoidSpawnerComponent, MapInitEvent>(OnMapInit,
            after: new []{ typeof(RandomMetadataSystem) });
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidSpawnerComponent component, MapInitEvent args)
    {
        // WWDP edit start
        if (component.SettingsPrototypeId == null)
        {
            QueueDel(uid);
            return;
        }

        var speciesList = _prototypeManager.EnumeratePrototypes<SpeciesPrototype>()
            .Where(x => !x.RoundStart)
            .Select(x => x.ID)
            .ToHashSet();
        var allSpeciesList = _prototypeManager.EnumeratePrototypes<SpeciesPrototype>()
            .Select(x => x.ID)
            .ToHashSet();

        _notRoundStartSpecies = speciesList;
        _allSpecies = allSpeciesList;

        QueueDel(uid);
        SpawnRandomHumanoid(component.SettingsPrototypeId, Transform(uid).Coordinates, MetaData(uid).EntityName);
        // WWDP edit end
    }

    public EntityUid SpawnRandomHumanoid(string prototypeId, EntityCoordinates coordinates, string name)
    {
        if (!_prototypeManager.TryIndex<RandomHumanoidSettingsPrototype>(prototypeId, out var prototype))
            throw new ArgumentException("Could not get random humanoid settings");

        // WWDP edit start - added whitelist
        var whitelist = prototype.SpeciesWhitelist;
        var blacklist = prototype.SpeciesBlacklist;

        var ignoredspecies = _allSpecies;

        if (whitelist.Any())
            ignoredspecies = ignoredspecies.Except(whitelist).ToHashSet();
        else if (blacklist.Any())
            ignoredspecies = blacklist.Union(_notRoundStartSpecies).ToHashSet();
        else
            ignoredspecies = _notRoundStartSpecies;

        var profile = HumanoidCharacterProfile.Random(ignoredspecies);
        // WWDP edit end
        var speciesProto = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var humanoid = EntityManager.CreateEntityUninitialized(speciesProto.Prototype, coordinates);

        _metaData.SetEntityName(humanoid, prototype.RandomizeName ? profile.Name : name);

        _humanoid.LoadProfile(humanoid, profile);

        if (prototype.Components != null)
        {
            foreach (var entry in prototype.Components.Values)
            {
                var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
                comp.Owner = humanoid; // This .owner must survive for now.
                EntityManager.RemoveComponent(humanoid, comp.GetType());
                EntityManager.AddComponent(humanoid, comp);
            }
        }

        EntityManager.InitializeAndStartEntity(humanoid);

        return humanoid;
    }
}
