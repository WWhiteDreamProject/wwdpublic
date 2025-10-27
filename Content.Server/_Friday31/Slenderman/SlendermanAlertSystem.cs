using Content.Shared._Friday31.Slenderman;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Friday31.Slenderman;

public sealed class SlendermanAlertSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlendermanAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlendermanAlertComponent, SlendermanAlertEvent>(OnAlert);
        SubscribeLocalEvent<SlendermanAlertComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, SlendermanAlertComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action);
        _actions.SetToggled(component.ActionEntity, component.IsActive);
    }

    private void OnAlert(EntityUid uid, SlendermanAlertComponent component, SlendermanAlertEvent args)
    {
        if (args.Handled)
            return;

        component.IsActive = !component.IsActive;

        if (component.IsActive)
        {
            StartAlert(uid, component);
        }
        else
        {
            StopAlert(uid, component);
        }

        _actions.SetToggled(component.ActionEntity, component.IsActive);

        Dirty(uid, component);
        args.Handled = true;
    }

    private void OnShutdown(EntityUid uid, SlendermanAlertComponent component, ComponentShutdown args)
    {
        StopAlert(uid, component);
    }

    private void StartAlert(EntityUid uid, SlendermanAlertComponent component)
    {
        StopAlert(uid, component);
        var audioParams = AudioParams.Default
            .WithLoop(true)
            .WithMaxDistance(component.MaxDistance)
            .WithReferenceDistance(component.ReferenceDistance)
            .WithVolume(5f);

        component.SoundStream = _audio.PlayPvs(component.AlertSound, uid, audioParams)?.Entity;
        Dirty(uid, component);
    }

    private void StopAlert(EntityUid uid, SlendermanAlertComponent component)
    {
        if (component.SoundStream != null)
        {
            _audio.Stop(component.SoundStream.Value);
            component.SoundStream = null;
            Dirty(uid, component);
        }
    }
}
