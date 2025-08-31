using Content.Shared.Destructible.Thresholds;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Magnet;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem
{
    private readonly List<SalvageMapPrototype> _salvageMaps = new();

    // WWDP EDIT START
    public enum SalvageMagnetOfferingTypeEnum
    {
        Asteroid,
        Debris,
        Salvage
    };

    private readonly Dictionary<SalvageMagnetOfferingTypeEnum, float> _offeringWeights = new() 
    {
        { SalvageMagnetOfferingTypeEnum.Asteroid, 4.5f },
        { SalvageMagnetOfferingTypeEnum.Debris, 3.5f },
        { SalvageMagnetOfferingTypeEnum.Salvage, 2.0f },
    };
    // WWDP EDIT END

    private readonly List<ProtoId<DungeonConfigPrototype>> _asteroidConfigs = new()
    {
        "BlobAsteroid",
        "ClusterAsteroid",
        "SpindlyAsteroid",
        "SwissCheeseAsteroid"
    };

    private readonly ProtoId<WeightedRandomPrototype> _asteroidOreWeights = "AsteroidOre";

    private readonly MinMax _asteroidOreCount = new(5, 7);

    private readonly List<ProtoId<DungeonConfigPrototype>> _debrisConfigs = new()
    {
        "ChunkDebris"
    };

    public ISalvageMagnetOffering GetSalvageOffering(int seed, SalvageMagnetOfferingTypeEnum? typeOverride = null) // WWDP EDIT
    {
        var rand = new System.Random(seed);

        var type = typeOverride ?? SharedRandomExtensions.Pick(_offeringWeights, rand);
        switch (type)
        {
            case SalvageMagnetOfferingTypeEnum.Asteroid:
                var configId = _asteroidConfigs[rand.Next(_asteroidConfigs.Count)];
                var configProto =_proto.Index(configId);
                var layers = new Dictionary<string, int>();

                var data = new DungeonData();
                data.Apply(configProto.Data);

                var config = new DungeonConfig
                {
                    Data = data,
                    Layers = new(configProto.Layers),
                    MaxCount = configProto.MaxCount,
                    MaxOffset = configProto.MaxOffset,
                    MinCount = configProto.MinCount,
                    MinOffset = configProto.MinOffset,
                    ReserveTiles = configProto.ReserveTiles
                };

                var count = _asteroidOreCount.Next(rand);
                var weightedProto = _proto.Index(_asteroidOreWeights);
                for (var i = 0; i < count; i++)
                {
                    var ore = weightedProto.Pick(rand);
                    config.Layers.Add(_proto.Index<OreDunGenPrototype>(ore));

                    var layerCount = layers.GetOrNew(ore);
                    layerCount++;
                    layers[ore] = layerCount;
                }

                return new AsteroidOffering
                {
                    Id = configId,
                    DungeonConfig = config,
                    MarkerLayers = layers,
                };

            case SalvageMagnetOfferingTypeEnum.Debris:
                var id = rand.Pick(_debrisConfigs);
                return new DebrisOffering
                {
                    Id = id
                };

            case SalvageMagnetOfferingTypeEnum.Salvage:
                // Salvage map seed
                _salvageMaps.Clear();
                _salvageMaps.AddRange(_proto.EnumeratePrototypes<SalvageMapPrototype>());
                _salvageMaps.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
                var mapIndex = rand.Next(_salvageMaps.Count);
                var map = _salvageMaps[mapIndex];

                return new SalvageOffering
                {
                    SalvageMap = map,
                };
            default:
                throw new NotImplementedException($"Salvage type {type} not implemented!");
        }
    }
}
