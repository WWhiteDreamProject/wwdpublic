namespace Content.Server.CombatMode.Disarm // WWDP moved to shared
{
    /// <summary>
    /// Applies a malus to disarm attempts against this item.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DisarmMalusComponent : Component
    {
        /// <summary>
        /// So, disarm chances are a % chance represented as a value between 0 and 1.
        /// This default would be a 30% penalty to that.
        /// </summary>
        [DataField("malus")]
        public float Malus = 0.3f;

        /// <summary>
        /// WWDP - Flat increase to the disarm malus when wielded in two hands
        /// </summary>
        [DataField]
        public float WieldedBonus = 0f;

        // WWDP
        [ViewVariables(VVAccess.ReadOnly)]
        public float CurrentMalus;
    }
}
