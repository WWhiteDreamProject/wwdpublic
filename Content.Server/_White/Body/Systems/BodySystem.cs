using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem : SharedBodySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ComponentGetState>(OnGetState);

        InitializeRelay();
    }

    #region Event Handling

    private void OnGetState(Entity<BodyComponent> ent, ref ComponentGetState args)
    {
        args.State = new BodyComponentState(ent.Comp);
    }

    #endregion
}
