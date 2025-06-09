namespace Content.Server._White.PreventGridLeave;

[RegisterComponent]
public sealed partial class PreventGridLeaveComponent : Component
{
    [DataField]
    public EntityUid? GridId;

    public bool IsTimerOn;

    public TimeSpan TimerStarted;

    [DataField]
    public int KillTimer = 30; // Seconds
}
