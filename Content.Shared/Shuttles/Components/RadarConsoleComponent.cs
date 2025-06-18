using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRadarConsoleSystem))]
public sealed partial class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeVV
    {
        get => MaxRange;
        set => IoCManager
            .Resolve<IEntitySystemManager>()
            .GetEntitySystem<SharedRadarConsoleSystem>()
            .SetRange(Owner, value, this);
    }

    [DataField, AutoNetworkedField]
    public float MaxRange = 256f;

    // WWDP EDIT START

    [ViewVariables, AutoNetworkedField]
    public float FieldOfView = MathF.Tau;

    [DataField("fieldOfVision")]
    public float FieldOfViewDegrees
    {
        get => FieldOfView / MathF.PI * 180;
        set => FieldOfView = value / 180 * MathF.PI;
    }
    // WWDP EDIT END

    /// <summary>
    /// If true, the radar will be centered on the entity. If not - on the grid on which it is located.
    /// </summary>
    [DataField]
    public bool FollowEntity = false;
}
