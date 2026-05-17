namespace Content.Shared.Destructible;

public abstract class SharedDestructibleSystem : EntitySystem
{
    // WD EDIT START
    /// <summary>
    /// Force entity to be destroyed and deleted.
    /// </summary>
    public bool DestroyEntity(Entity<MetaDataComponent?> ent)
    {
        var destructionAttemptEv = new DestructionAttemptEvent();
        RaiseLocalEvent(ent, destructionAttemptEv);
        if (destructionAttemptEv.Cancelled)
            return false;

        var destructionEv = new DestructionEventArgs();
        RaiseLocalEvent(ent, destructionEv);

        PredictedQueueDel(ent);
        return true;
    }
    // WD EDIT END

    /// <summary>
    ///     Force entity to break.
    /// </summary>
    public void BreakEntity(EntityUid owner)
    {
        var eventArgs = new BreakageEventArgs();
        RaiseLocalEvent(owner, eventArgs);
    }
}

// WD EDIT START
/// <summary>
/// Raised before an entity is about to be destroyed and deleted
/// </summary>
public sealed class DestructionAttemptEvent : CancellableEntityEventArgs;
// WD EDIT END

/// <summary>
///     Raised when entity is destroyed and about to be deleted.
/// </summary>
public sealed class DestructionEventArgs : EntityEventArgs
{

}

/// <summary>
///     Raised when entity was heavy damage and about to break.
/// </summary>
public sealed class BreakageEventArgs : EntityEventArgs
{

}
