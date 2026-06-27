using Content.Shared.Actions;

namespace Content.Shared._White.Abilities.Invoker;

public sealed partial class InvokerOrbActionEvent : InstantActionEvent
{
    public OrbType Orb { get; set; } = OrbType.Quas;
}
