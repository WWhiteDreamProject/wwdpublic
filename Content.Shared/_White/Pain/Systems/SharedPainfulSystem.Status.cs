using Content.Shared._White.Pain.Components;

namespace Content.Shared._White.Pain.Systems;

public abstract partial class SharedPainfulSystem
{
    private void InitializeStatus()
    {
        SubscribeLocalEvent<PainStatusComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PainStatusComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PainStatusComponent, PainLevelChangedEvent>(OnPainLevelChanged);
    }

    #region Event Handling

    private void OnShutdown(Entity<PainStatusComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void OnStartup(Entity<PainStatusComponent> ent, ref ComponentStartup args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert);
    }

    private void OnPainLevelChanged(Entity<PainStatusComponent> ent, ref PainLevelChangedEvent args)
    {
        if (!ent.Comp.PainStatus.ContainsKey(args.Location))
            return;

        ent.Comp.PainStatus[args.Location] = args.Level;
        Dirty(ent);
    }

    #endregion
}
