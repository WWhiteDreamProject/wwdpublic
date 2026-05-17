using Content.Shared._White.Nutrition.Prototypes;
using Content.Shared._White.Nutrition.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.Nutrition.Components;

/// <summary>
/// This is used on an entity with a solution container to flag a specific solution as being able to have its
/// reagents consumed directly.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(IngestionSystem))]
public sealed partial class IngestibleComponent : Component
{
    /// <summary>
    /// Should this entity be deleted when our solution is emptied?
    /// </summary>
    [DataField]
    public bool DeleteOnEmpty = true;

    /// <summary>
    /// For mobs that are food, requires killing them before eating.
    /// </summary>
    [DataField]
    public bool RequireDead = true;

    /// <summary>
    /// Do we need a utensil to access this solution?
    /// </summary>
    [DataField]
    public bool UtensilRequired;

    /// <summary>
    /// How much of our solution is eaten on a do-after completion. Set to null to eat the whole thing.
    /// </summary>
    [DataField]
    public FixedPoint2? TransferAmount = FixedPoint2.New(5);

    /// <summary>
    /// Trash we spawn when eaten will not spawn if the item isn't deleted when empty.
    /// </summary>
    [DataField]
    public List<EntProtoId> Trashes = new();

    /// <summary>
    /// The localization identifier for the ingestion message.
    /// </summary>
    [DataField]
    public LocId Message;

    /// <summary>
    /// The localization identifier for an observer's or "others'" ingestion message.
    /// </summary>
    [DataField]
    public LocId OtherMessage;

    /// <summary>
    /// What type of food are we, currently used for determining verbs and some checks?
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// Localization noun used when consuming this item.
    /// </summary>
    [DataField]
    public LocId Noun;

    /// <summary>
    /// Localization verb used when consuming this item.
    /// </summary>
    [DataField]
    public LocId Verb;

    /// <summary>
    /// Verb, icon, and sound data for our edible.
    /// </summary>
    [DataField]
    public ProtoId<IngestiblePrototype> Edible = "Food";

    /// <summary>
    /// How long it takes to eat the food personally.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// This is how many seconds it takes to force-feed someone this food.
    /// Should probably be smaller for small items like pills.
    /// </summary>
    [DataField]
    public TimeSpan ForceFeedDelay = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// The key name of the solution that stores the consumable reagents.
    /// </summary>
    [DataField]
    public string SolutionName = "food";

    /// <summary>
    /// The sound we make when eaten.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("eating");

    /// <summary>
    /// An icon used to represent an ingesting verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon;

    /// <summary>
    /// Acceptable utensils to use.
    /// </summary>
    [DataField]
    public UtensilType Utensil = UtensilType.Fork;

    /// <summary>
    /// Reference to the entity's edible solution.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;
}
