using Content.Shared._White.Body.Pain.Components;
using Content.Shared._White.Body.Pain.Systems;
using Robust.Shared.GameStates;

namespace Content.Server._White.Body.Pain.Systems;

public sealed class PainSystem : SharedPainSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PainfulComponent, ComponentGetState>(OnGetState);
    }

    #region Event Handling

    private void OnGetState(Entity<PainfulComponent> painful, ref ComponentGetState args) =>
        args.State = new PainfulComponentState(painful.Comp);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PainfulComponent>();
        while (query.MoveNext(out var uid, out var painful))
        {
            if (painful.LastUpdate + painful.CurrentUpdateInterval >= GameTiming.CurTime)
                continue;

            UpdatePain((uid, painful));
        }
    }

    #endregion
}
