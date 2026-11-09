using Content.Shared._White.Mobs.Systems;
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
        SubscribeLocalEvent<PainThresholdsComponent, GetMobStateOverlayLevelEvent>(OnGetMobStateOverlayLevel);
        SubscribeLocalEvent<PainThresholdsComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PainThresholdsComponent, PainChangedEvent>(OnPainChanged);
        SubscribeLocalEvent<PainThresholdsComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    #region Event Handling

    protected virtual void OnShutdown(Entity<PainThresholdsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent, ent.Comp.AlertCategory);
    }

    private void OnStartup(Entity<PainThresholdsComponent> ent, ref ComponentStartup args)
    {
        var pain = GetPain(ent.Owner);

        UpdateBlend(ent, pain);
        UpdateMobState(ent, pain);
        UpdatePainLevel(ent, pain);

        UpdateAlert(ent);
    }

    private void OnGetMobStateOverlayLevel(Entity<PainThresholdsComponent> ent, ref GetMobStateOverlayLevelEvent args)
    {
        args.Level = MathF.Max(args.Level, ent.Comp.Blend);
    }

    private void OnMobStateChanged(Entity<PainThresholdsComponent> ent, ref MobStateChangedEvent args)
    {
        var pain = GetPain(ent.Owner);
        UpdateBlend(ent, pain);

        UpdateAlert(ent);
    }

    protected virtual void OnPainChanged(Entity<PainThresholdsComponent> ent, ref PainChangedEvent args)
    {
        UpdateBlend(ent, args.Painful.Comp.CurrentPain);
        UpdateMobState(ent, args.Painful.Comp.CurrentPain);
        UpdatePainLevel(ent, args.Painful.Comp.CurrentPain);

        UpdateAlert(ent);
    }

    private void OnUpdateMobState(Entity<PainThresholdsComponent> ent, ref UpdateMobStateEvent args)
    {
        args.State = ent.Comp.MobState;
    }

    #endregion

    #region Private API

    private void UpdateAlert(Entity<PainThresholdsComponent> ent)
    {
        if (!ent.Comp.StateAlerts.TryGetValue(ent.Comp.MobState, out var alert))
        {
            Log.Error($"No alert for mob state {ent.Comp.MobState} for entity {ToPrettyString(ent)}");
            return;
        }

        var severity = (short) MathF.Round(MathHelper.Lerp(0, _alerts.GetMaxSeverity(alert), ent.Comp.Blend));
        _alerts.ShowAlert(ent, alert, severity);
    }

    public void UpdateBlend(Entity<PainThresholdsComponent> ent, FixedPoint2 pain)
    {
        ent.Comp.Blend = 0f;
        DirtyField(ent, ent.Comp, nameof(PainThresholdsComponent.Blend));

        if (!ent.Comp.MobStateThresholds.TryGetNextValue(ent.Comp.MobState, out var nextState))
            return;

        if (!ent.Comp.MobStateThresholds.TryGetKey(nextState, out var threshold))
            return;

        ent.Comp.Blend = FixedPoint2.Clamp(pain / threshold, 0, 1).Float();
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
        var painLevel = ent.Comp.LevelThresholds.HighestMatch(pain) ?? PainLevel.Zero;
        if (ent.Comp.Level == painLevel)
            return;

        ent.Comp.Level = painLevel;
        DirtyField(ent, ent.Comp, nameof(PainThresholdsComponent.Level));

        var ev = new PainLevelChangedEvent(painLevel);
        RaiseLocalEvent(ent, ref ev);
    }

    #endregion
}
