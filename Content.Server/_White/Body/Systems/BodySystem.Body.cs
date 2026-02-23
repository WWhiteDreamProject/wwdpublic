using Content.Shared._White.Body.Components;
using Robust.Shared.GameStates;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem
{
    private void InitializeBody() =>
        SubscribeLocalEvent<BodyComponent, ComponentGetState>(OnBodyGetState);

    #region Event Handling

    private void OnBodyGetState(Entity<BodyComponent> body, ref ComponentGetState args) =>
        args.State = new BodyComponentState(body.Comp, EntityManager);

    #endregion
}
