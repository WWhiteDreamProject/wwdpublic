using System.Collections.Frozen;
using System.Linq;
using System.Text.Json.Serialization;
using Content.Shared._White.Body.Prototypes;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityEffects;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Reagent
{
    [Prototype("reagent")]
    [DataDefinition]
    public sealed partial class ReagentPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField(required: true)]
        private LocId Name { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        [DataField]
        public string Group { get; private set; } = "Unknown";

        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ReagentPrototype>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataField]
        public bool Abstract { get; private set; }

        [DataField("desc", required: true)]
        private LocId Description { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedDescription => Loc.GetString(Description);

        [DataField("physicalDesc", required: true)]
        private LocId PhysicalDescription { get; set; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedPhysicalDescription => Loc.GetString(PhysicalDescription);

        /// <summary>
        ///     Is this reagent recognizable to the average spaceman (water, welding fuel, ketchup, etc)?
        /// </summary>
        [DataField]
        public bool Recognizable;

        [DataField]
        public ProtoId<FlavorPrototype>? Flavor;

        /// <summary>
        /// There must be at least this much quantity in a solution to be tasted.
        /// </summary>
        [DataField]
        public FixedPoint2 FlavorMinimum = FixedPoint2.New(0.1f);

        [DataField("color")]
        public Color SubstanceColor { get; private set; } = Color.White;

        /// <summary>
        ///     The specific heat of the reagent.
        ///     How much energy it takes to heat one unit of this reagent by one Kelvin.
        /// </summary>
        [DataField]
        public float SpecificHeat { get; private set; } = 1.0f;

        [DataField]
        public float? BoilingPoint { get; private set; }

        [DataField]
        public float? MeltingPoint { get; private set; }

        [DataField]
        public SpriteSpecifier? MetamorphicSprite { get; private set; } = null;

        [DataField]
        public int MetamorphicMaxFillLevels { get; private set; } = 0;

        [DataField]
        public string? MetamorphicFillBaseName { get; private set; } = null;

        [DataField]
        public bool MetamorphicChangeColor { get; private set; } = true;

        /// <summary>
        /// If this reagent is part of a puddle is it slippery.
        /// </summary>
        [DataField]
        public bool Slippery;

        /// <summary>
        /// How easily this reagent becomes fizzy when aggitated.
        /// 0 - completely flat, 1 - fizzes up when nudged.
        /// </summary>
        [DataField]
        public float Fizziness;

        /// <summary>
        /// How much reagent slows entities down if it's part of a puddle.
        /// 0 - no slowdown; 1 - can't move.
        /// </summary>
        [DataField]
        public float Viscosity;

        /// <summary>
        /// Should this reagent work on the dead?
        /// </summary>
        [DataField]
        public bool WorksOnTheDead;

        [DataField(serverOnly: true)]
        public FrozenDictionary<ProtoId<MetabolismStagePrototype>, ReagentEffectsEntry>? Metabolisms; // WD EDIT

        [DataField(serverOnly: true)]
        public Dictionary<ProtoId<ReactiveGroupPrototype>, ReactiveReagentEffectEntry>? ReactiveEffects;

        [DataField(serverOnly: true)]
        public List<ITileReaction> TileReactions = new(0);

        [DataField("plantMetabolism", serverOnly: true)]
        public List<EntityEffect> PlantMetabolisms = new(0);

        [DataField]
        public float PricePerUnit;

        [DataField]
        public SoundSpecifier FootstepSound = new SoundCollectionSpecifier("FootstepWater", AudioParams.Default.WithVolume(6));

        public FixedPoint2 ReactionTile(TileRef tile, FixedPoint2 reactVolume, IEntityManager entityManager, List<ReagentData>? data)
        {
            var removed = FixedPoint2.Zero;

            if (tile.Tile.IsEmpty)
                return removed;

            foreach (var reaction in TileReactions)
            {
                removed += reaction.TileReact(tile, this, reactVolume - removed, entityManager, data);

                if (removed > reactVolume)
                    throw new Exception("Removed more than we have!");

                if (removed == reactVolume)
                    break;
            }

            return removed;
        }

        public void ReactionPlant(EntityUid? plantHolder, ReagentQuantity amount, Solution solution)
        {
            if (plantHolder == null)
                return;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var random = IoCManager.Resolve<IRobustRandom>();
            var args = new EntityEffectReagentArgs(plantHolder.Value, entMan, null, solution, amount.Quantity, this, null, 1f);
            foreach (var plantMetabolizable in PlantMetabolisms)
            {
                if (!plantMetabolizable.ShouldApply(args, random))
                    continue;

                if (plantMetabolizable.ShouldLog)
                {
                    var entity = args.TargetEntity;
                    entMan.System<SharedAdminLogSystem>().Add(LogType.ReagentEffect, plantMetabolizable.LogImpact,
                        $"Plant metabolism effect {plantMetabolizable.GetType().Name:effect} of reagent {ID:reagent} applied on entity {entMan.ToPrettyString(entity):entity} at {entMan.GetComponent<TransformComponent>(entity).Coordinates:coordinates}");
                }

                plantMetabolizable.Effect(args);
            }
        }
    }

    [Serializable, NetSerializable]
    public struct ReagentGuideEntry
    {
        public string ReagentPrototype;

        public Dictionary<ProtoId<MetabolismStagePrototype>, ReagentEffectsGuideEntry>? GuideEntries; // WD EDIT

        public List<string>? PlantMetabolisms = null;

        public ReagentGuideEntry(ReagentPrototype proto, IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            ReagentPrototype = proto.ID;
            GuideEntries = proto.Metabolisms?
                .Select(x => (x.Key, x.Value.MakeGuideEntry(prototype, entSys)))
                .ToDictionary(x => x.Key, x => x.Item2);
            if (proto.PlantMetabolisms.Count > 0)
            {
                PlantMetabolisms = new List<string> (proto.PlantMetabolisms
                    .Select(x => x.GuidebookEffectDescription(prototype, entSys))
                    .Where(x => x is not null)
                    .Select(x => x!)
                    .ToArray());
            }
        }
    }


    [DataDefinition]
    public sealed partial class ReagentEffectsEntry
    {
        /// <summary>
        ///     Amount of reagent to metabolize, per metabolism cycle.
        /// </summary>
        [JsonPropertyName("rate")]
        [DataField("metabolismRate")]
        public FixedPoint2 MetabolismRate = FixedPoint2.New(0.5f);

        /// <summary>
        ///     A list of effects to apply when these reagents are metabolized.
        /// </summary>
        [JsonPropertyName("effects")]
        // WD EDIT START
        [DataField]
        public EntityEffect[] Effects = Array.Empty<EntityEffect>();

        /// <summary>
        ///     Ratio of this reagent to metabolites for transfer to the next solution by a metabolizer
        /// </summary>
        [DataField]
        public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Metabolites = new();
        // WD EDIT END

        public ReagentEffectsGuideEntry MakeGuideEntry(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return new ReagentEffectsGuideEntry(MetabolismRate,
                Effects
                    .Select(x => x.GuidebookEffectDescription(prototype, entSys)) // hate.
                    .Where(x => x is not null)
                    .Select(x => x!)
                    .ToArray(),
                Metabolites); // WD EDIT
        }
    }

    [Serializable, NetSerializable]
    // WD EDIT START
    public struct ReagentEffectsGuideEntry(
        FixedPoint2 metabolismRate,
        string[] effectDescriptions,
        Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> metabolites)
    {
        public FixedPoint2 MetabolismRate = metabolismRate;

        public string[] EffectDescriptions = effectDescriptions;

        public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Metabolites = metabolites;
    }
    // WD EDIT END

    [DataDefinition]
    public sealed partial class ReactiveReagentEffectEntry
    {
        [DataField("methods", required: true)]
        public HashSet<ReactionMethod> Methods = default!;

        [DataField("effects", required: true)]
        public EntityEffect[] Effects = default!;
    }
}
