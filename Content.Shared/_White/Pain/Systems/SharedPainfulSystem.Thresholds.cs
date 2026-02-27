using Content.Shared._White.Pain.Components;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._White.Pain.Systems;

public abstract partial class SharedPainfulSystem
{
    private void InitializeThresholds()
    {
        SubscribeLocalEvent<PainThresholdsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PainThresholdsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PainThresholdsComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PainThresholdsComponent, PainChangedEvent>(OnPainChanged);
        SubscribeLocalEvent<PainThresholdsComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    #region Event Handling

    private void OnShutdown(Entity<PainThresholdsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent, ent.Comp.AlertCategory);
    }

    private void OnStartup(Entity<PainThresholdsComponent> ent, ref ComponentStartup args)
    {
        var pain = GetPain(ent.Owner);

        UpdateMobState(ent, pain);
        UpdatePainLevel(ent, pain);
        UpdateAlert(ent, pain);
    }

    private void OnMobStateChanged(Entity<PainThresholdsComponent> ent, ref MobStateChangedEvent args)
    {
        var pain = GetPain(ent.Owner);
        UpdateAlert(ent, pain);
    }

    private void OnPainChanged(Entity<PainThresholdsComponent> ent, ref PainChangedEvent args)
    {
        UpdateMobState(ent, args.Painful.Comp.CurrentPain);
        UpdatePainLevel(ent, args.Painful.Comp.CurrentPain);
        UpdateAlert(ent, args.Painful.Comp.CurrentPain);
    }

    private void OnUpdateMobState(Entity<PainThresholdsComponent> ent, ref UpdateMobStateEvent args)
    {
        args.State = ent.Comp.MobState;
    }

    #endregion

    #region Private API

    private void UpdateAlert(Entity<PainThresholdsComponent> ent, FixedPoint2 pain)
    {
        if (!ent.Comp.StateAlerts.TryGetValue(ent.Comp.MobState, out var alert))
        {
            Log.Error($"No alert for mob state {ent.Comp.MobState} for entity {ToPrettyString(ent)}");
            return;
        }

        var severity = _alerts.GetMinSeverity(alert);
        if (ent.Comp.MobStateThresholds.TryGetNextValue(ent.Comp.MobState, out var nextState)
            && ent.Comp.MobStateThresholds.TryGetKey(nextState, out var threshold))
        {
            var blend = FixedPoint2.Clamp(pain / threshold, 0, 1).Float();

            severity = (short) MathF.Round(MathHelper.Lerp(severity, _alerts.GetMaxSeverity(alert), blend));
        }

        _alerts.ShowAlert(ent, alert, severity);
    }

    public void UpdateMobState(Entity<PainThresholdsComponent> ent, FixedPoint2 pain)
    {
        var mobState = ent.Comp.MobStateThresholds.HighestMatch(pain) ?? MobState.Alive;
        if (ent.Comp.MobState == mobState)
            return;

        ent.Comp.MobState = mobState;
        DirtyField(ent, ent.Comp, nameof(PainThresholdsComponent.MobState));

        _mobState.UpdateMobState(ent);
    }

    public void UpdatePainLevel(Entity<PainThresholdsComponent> ent, FixedPoint2 pain)
    {
        var painLevel = ent.Comp.PainLevelThresholds.HighestMatch(pain) ?? PainLevel.Zero;
        if (ent.Comp.PainLevel == painLevel)
            return;

        ent.Comp.PainLevel = painLevel;
        DirtyField(ent, ent.Comp, nameof(PainThresholdsComponent.PainLevel));

        RaiseLocalEvent(ent, new PainLevelChangedEvent(painLevel));
    }

    #endregion
}
