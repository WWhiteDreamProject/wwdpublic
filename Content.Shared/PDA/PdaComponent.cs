using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PDA
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)] // WD EDIT
    public sealed partial class PdaComponent : Component
    {
        public const string PdaIdSlotId = "PDA-id";
        public const string PdaPenSlotId = "PDA-pen";
        public const string PdaPaiSlotId = "PDA-pai";
        public const string PdaPassportSlotId = "PDA-passport";

        [DataField]
        public ItemSlot IdSlot = new();

        [DataField]
        public ItemSlot PenSlot = new();

        [DataField]
        public ItemSlot PaiSlot = new();

        [DataField]
        public ItemSlot PassportSlot = new();

        // Really this should just be using ItemSlot.StartingItem. However, seeing as we have so many different starting
        // PDA's and no nice way to inherit the other fields from the ItemSlot data definition, this makes the yaml much
        // nicer to read.
        [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? IdCard;

        [ViewVariables] public EntityUid? ContainedId;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables(VVAccess.ReadWrite)] public string? OwnerName;
        // The Entity that "owns" the PDA, usually a player's character.
        // This is useful when we are doing stuff like renaming a player and want to find their PDA to change the name
        // as well.
        [ViewVariables(VVAccess.ReadWrite)] public EntityUid? PdaOwner;
        [ViewVariables] public string? StationName;
        [ViewVariables] public string? StationAlertLevel;
        [ViewVariables] public Color StationAlertColor = Color.White;

        // WD EDIT START
        public const string AnimationKey = "pda_animation";

        /// <summary>
        /// The current state of the pda.
        /// </summary>
        [DataField, AutoNetworkedField]
        public PdaState State = PdaState.Closed;

        /// <summary>
        /// The sprite state used for the pda when it's closed.
        /// </summary>
        [DataField]
        public string ClosedSpriteState = "closed";

        /// <summary>
        /// The sprite state used for the pda when it's closing.
        /// </summary>
        [DataField]
        public string ClosingSpriteState = "closing";

        /// <summary>
        /// The sprite state used for the pda when it's open.
        /// </summary>
        [DataField]
        public string OpenSpriteState = "open";

        /// <summary>
        /// The sprite state used for the pda when it's opening.
        /// </summary>
        [DataField]
        public string OpeningSpriteState = "opening";

        /// <summary>
        /// The sprite state used for the pda screen when it's closed.
        /// </summary>
        [DataField]
        public string ScreenClosedSpriteState = "screen";

        /// <summary>
        /// The sprite state used for the pda screen when it's closing.
        /// </summary>
        [DataField]
        public string ScreenClosingSpriteState = "screen_closing";

        /// <summary>
        /// The sprite state used for the pda screen when it's open.
        /// </summary>
        [DataField]
        public string ScreenOpenSpriteState = "screen";

        /// <summary>
        /// The sprite state used for the pda screen when it's opening.
        /// </summary>
        [DataField]
        public string ScreenOpeningSpriteState = "screen_opening";

        /// <summary>
        /// The length of the pda's opening animation.
        /// </summary>
        [DataField]
        public TimeSpan OpeningAnimationTime = TimeSpan.FromSeconds(1.7f);

        /// <summary>
        /// The length of the pda's closing animation.
        /// </summary>
        [DataField]
        public TimeSpan ClosingAnimationTime = TimeSpan.FromSeconds(1.7f);

        /// <summary>
        /// The animation used when the pda closes.
        /// </summary>
        [ViewVariables]
        public object ClosingAnimation = null!;

        /// <summary>
        /// The animation used when the pda opens.
        /// </summary>
        [ViewVariables]
        public object OpeningAnimation = null!;

        /// <summary>
        /// This is the time when the state will next update.
        /// </summary>
        [ViewVariables, AutoNetworkedField]
        public TimeSpan? NextStateChange;
        // WD EDIT END
    }
}
