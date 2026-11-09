using Content.Shared._White.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;


namespace Content.Shared._White.Mobs.Systems;

public abstract class SharedMobStateOverlaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateOverlayComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MobStateOverlayComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    #region Event Handling

    private void OnGetState(Entity<MobStateOverlayComponent> ent, ref ComponentGetState args)
    {
        args.State = new MobStateOverlayState(ent.Comp);
    }

    protected virtual void OnMobStateChanged(Entity<MobStateOverlayComponent> ent, ref MobStateChangedEvent args)
    {
        ent.Comp.State = args.NewMobState;
        Dirty(ent);
    }

    #endregion
}

/// <summary>
/// Event raised to calculate visual overlay level.
/// </summary>
[ByRefEvent]
public record struct GetMobStateOverlayLevelEvent(MobState State)
{
    /// <summary>
    /// The intensity level of the overlay.
    /// </summary>
    public float Level;
}
