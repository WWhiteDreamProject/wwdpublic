using Content.Shared.Actions;

namespace Content.Shared._White.Abilities.Invoker;

public sealed partial class InvokerOrbActionEvent : InstantActionEvent
{
    [DataField]
    public OrbType Orb { get; set; } = OrbType.None;
}

public sealed partial class InvokerInvokeActionEvent : InstantActionEvent;
