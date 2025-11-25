using Content.Shared._White.Body.Components;
using Robust.Shared.GameStates;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeBone() =>
        SubscribeLocalEvent<BoneComponent, ComponentGetState>(OnBoneGetState);

    #region Event Handling

    private void OnBoneGetState(Entity<BoneComponent> bone, ref ComponentGetState args) =>
        args.State = new BoneComponentState(bone.Comp, EntityManager);

    #endregion
}
