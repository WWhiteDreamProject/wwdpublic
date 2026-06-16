using Content.Shared.RadialSelector;
using Robust.Shared.GameStates;

namespace Content.Shared._White.BloodCult.TimedFactory;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TimedFactoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = new();

    [DataField]
    public float Cooldown = 240;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float CooldownRemain = 0;
}
