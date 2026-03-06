using Content.Shared._White.Body.Components;


namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Base class for all throw events.
    /// </summary>
    public abstract class ThrowEvent : HandledEntityEventArgs
    {
        ///Nyano - Summary: Allows us to tell who threw the item. It matters!
        /// <summary>
        ///     The entity that threw <see cref="Thrown"/>.
        /// </summary>
        public EntityUid? User { get; }
        // End Nyano code.
        public readonly EntityUid Thrown;
        public readonly EntityUid Target;
        public ThrownItemComponent Component;
        public BodyPartType BodyPartType; // WD EDIT

        public ThrowEvent(EntityUid? user, EntityUid thrown, EntityUid target, ThrownItemComponent component, BodyPartType bodyPartType) // WD EDIT
        {
            User = user; //Nyano - Summary: User added.
            Thrown = thrown;
            Target = target;
            Component = component;
            BodyPartType = bodyPartType; // WD EDIT
        }
    }

    /// <summary>
    ///     Raised directed on the target entity being hit by the thrown entity.
    /// </summary>
    public sealed class ThrowHitByEvent : ThrowEvent
    {
        public ThrowHitByEvent(EntityUid? user, EntityUid thrown, EntityUid target, ThrownItemComponent component, BodyPartType bodyPartType) : base(user, thrown, target, component, bodyPartType) // WD EDIT
        {
        }
    }

    /// <summary>
    ///     Raised directed on the thrown entity that hits another.
    /// </summary>
    public sealed class ThrowDoHitEvent : ThrowEvent
    {
        public ThrowDoHitEvent(EntityUid thrown, EntityUid target, ThrownItemComponent component, BodyPartType bodyPartType) : base(null, thrown, target, component, bodyPartType) // WD EDIT
        {
        }
    }
}
