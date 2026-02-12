namespace Content.Server.DeviceLinking.Events
{
    [ByRefEvent]
    public readonly record struct SignalTimerStartedEvent;

    [ByRefEvent]
    public readonly record struct SignalTimerTriggeredEvent;

    [ByRefEvent]
    public readonly record struct SignalTimerCancelledEvent;
}
