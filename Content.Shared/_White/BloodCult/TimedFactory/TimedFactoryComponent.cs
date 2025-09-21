using Content.Shared.RadialSelector;

namespace Content.Shared._White.BloodCult.TimedFactory;

[RegisterComponent]
public sealed partial class TimedFactoryComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = new();

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(240);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CooldownIn = TimeSpan.Zero;
}
