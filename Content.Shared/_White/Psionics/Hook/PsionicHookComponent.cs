using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._White.Psionics;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PsionicHookComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Projectile;

    [DataField, ViewVariables]
    public SpriteSpecifier HookJointSpite =
        new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectilePsionicHookComponent : Component
{
    [DataField]
    public float BasePower = 20f;

    [DataField, AutoNetworkedField]
    public bool IsReturning = false;

    [DataField, AutoNetworkedField]
    public EntityUid Gun;

    [DataField, AutoNetworkedField]
    public float ReturnSpeed = 15f;
}
