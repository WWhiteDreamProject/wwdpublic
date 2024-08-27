using Content.Shared.Movement.Pulling.Systems;

namespace Content.Shared._White.Intent.Event;

public sealed class CheckGrabOverridesEvent : EntityEventArgs
{
    public CheckGrabOverridesEvent(GrabStage stage)
    {
        Stage = stage;
    }

    public GrabStage Stage { get; set; }
}
