namespace Content.Shared._White.Hearing;

public sealed class HearingChangedEvent : EntityEventArgs
{
    public EntityUid Target;

    public bool Undeafen;

    public bool Permanent;

    public float Duration;

    public LocId DeafChatMessage;

    public HearingChangedEvent(EntityUid target, bool undeafen)
    {
        Target = target;
        Undeafen = undeafen;
        Permanent = false;
        Duration = 0f;
        DeafChatMessage = "";
    }

    public HearingChangedEvent(EntityUid target, bool undeafen, bool permanent, float duration, LocId deafChatMessage)
    {
        Target = target;
        Undeafen = undeafen;
        Permanent = permanent;
        Duration = duration;
        DeafChatMessage = deafChatMessage;
    }
}
