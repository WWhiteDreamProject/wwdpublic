using Content.Shared._White.Body.Components;
using Robust.Shared.GameStates;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeBodyPart() =>
        SubscribeLocalEvent<BodyPartComponent, ComponentGetState>(OnBodyPartGetState);

    #region Event Handling

    private void OnBodyPartGetState(Entity<BodyPartComponent> bodyPart, ref ComponentGetState args) =>
        args.State = new BodyPartComponentState(bodyPart.Comp, EntityManager);

    #endregion
}
