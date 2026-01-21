using Robust.Shared.GameStates;

namespace Content.Shared._NC.Trauma.Components
{
    /// <summary>
    /// Вешается на игроков (мобов). Хранит статус их подписки.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class TraumaSubscriberComponent : Component
    {
        // По умолчанию ставим Bronze, как ты просил.
        [DataField("tier")]
        public TraumaSubscriptionTier Tier = TraumaSubscriptionTier.Bronze;
    }
}