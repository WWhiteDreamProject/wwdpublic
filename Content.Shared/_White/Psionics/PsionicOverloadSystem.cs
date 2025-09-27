namespace Content.Shared._White.Psionics;

public sealed class PsionicOverloadEvent : EntityEventArgs
{
    public EntityUid User { get; }
    
    public PsionicOverloadEvent(EntityUid user)
    {
        User = user;
    }
}