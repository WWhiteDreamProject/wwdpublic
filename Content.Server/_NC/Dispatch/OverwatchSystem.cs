using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._NC.Dispatch;
using Content.Shared._NC.Dispatch.Components;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.SurveillanceCamera;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Content.Server.SurveillanceCamera;
using Content.Server.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Server.Interaction;
using Content.Shared.Paper;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.GameObjects;

namespace Content.Server._NC.Dispatch
{
    public sealed class OverwatchSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly InteractionSystem _interaction = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SurveillanceCameraSystem _cameraSystem = default!;
        [Dependency] private readonly SurveillanceCameraMonitorSystem _cameraMonitorSystem = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        private const float GunshotCooldown = 30f; // seconds per camera

        public override void Initialize()
        {
            base.Initialize();

            // Listen to every gunshot in the world so we can forward it to sensors
            SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);


            // UI messages from consoles
            Subs.BuiEvents<OverwatchConsoleComponent>(OverwatchConsoleUiKey.Key, subs => {
                subs.Event<OverwatchAlertActionMessage>(OnAlertAction);
            });
            SubscribeLocalEvent<OverwatchConsoleComponent, BoundUIOpenedEvent>(OnUiOpen);

            // Listen for sensor destruction (Connection Lost)
            SubscribeLocalEvent<AcousticSensorComponent, ComponentShutdown>(OnSensorShutdown);
        }

        private void OnSensorShutdown(EntityUid uid, AcousticSensorComponent component, ComponentShutdown args)
        {
            if (!TryComp<SurveillanceCameraComponent>(uid, out var surv))
                return;

            var sector = surv.CameraId ?? uid.ToString();
            AddAlert(uid, "ПОТЕРЯ СВЯЗИ", sector, false);
        }

        private void OnUiOpen(EntityUid uid, OverwatchConsoleComponent comp, BoundUIOpenedEvent args)
        {
            UpdateConsoleUi(uid, comp);
        }

        private void OnAlertAction(Entity<OverwatchConsoleComponent> ent, ref OverwatchAlertActionMessage msg)
        {
            var uid = ent.Owner;
            var comp = ent.Comp;
            if (!comp.ActiveAlerts.TryGetValue(msg.AlertId, out var alert))
                return;

            switch (msg.Action)
            {
                case OverwatchAlertAction.ConnectCamera:
                {
                    // open the surveillance camera monitor UI for that user on this console
                    _ui.TryOpenUi(uid, SurveillanceCameraMonitorUiKey.Key, msg.Actor);

                    // switch the console's monitor to the selected camera
                    _cameraMonitorSystem.SwitchCameraToUid(uid, EntityManager.GetEntity(alert.CameraUid));
                    break;
                }
                case OverwatchAlertAction.PrintTicket:
                    SpawnTicket(uid, alert);
                    break;
                case OverwatchAlertAction.Archive:
                    comp.ActiveAlerts.Remove(msg.AlertId);
                    break;
            }

            UpdateConsoleUi(uid, comp);
        }

        private void SpawnTicket(EntityUid uid, OverwatchAlertData alert)
        {
            // spawn a physical dispatch ticket and put a description on it
            var ticket = EntityManager.SpawnEntity("DispatchCallTicket", Transform(uid).Coordinates);
            if (TryComp<PaperComponent>(ticket, out var paper))
            {
                paper.Content = $"Официальный бланк NCPD. Зафиксирована {alert.Type.ToLower()} в секторе: {alert.Sector}. Время: {alert.TimeStr}";
                Dirty(ticket, paper);
            }
        }

        private void UpdateConsoleUi(EntityUid uid, OverwatchConsoleComponent comp)
        {
            var list = new List<OverwatchAlertData>(comp.ActiveAlerts.Values);
            _ui.SetUiState(uid, OverwatchConsoleUiKey.Key, new OverwatchConsoleState(list));
        }

        private void OnGunShot(EntityUid uid, GunComponent component, GunShotEvent ev)
        {
            // Determine whether the gunshot sound should be treated as suppressed.
            bool suppressed = false;
            if (component.SoundGunshot is SoundPathSpecifier path && path.Path.CanonPath.Contains("silenced"))
                suppressed = true;
            else if (component.SoundGunshot is SoundCollectionSpecifier col && col.Collection?.ToLowerInvariant().Contains("silenced") == true)
                suppressed = true;

            var shooter = ev.User;
            var originPos = _transform.ToMapCoordinates(Transform(shooter).Coordinates);
            DispatchAcousticEvent(originPos, suppressed, shooter);
        }


        private void DispatchAcousticEvent(MapCoordinates origin, bool suppressed, EntityUid? shooter)
        {
            var mapPos = origin;
            // iterate over all sensors
            var query = EntityQueryEnumerator<AcousticSensorComponent, TransformComponent, SurveillanceCameraComponent>();
            while (query.MoveNext(out var camUid, out var sensor, out var xform, out var surv))
            {
                if (!sensor.Enabled)
                    continue;

                // camera must be powered / active
                if (!surv.Active)
                    continue;

                float maxRange = suppressed ? sensor.SuppressedRange : sensor.GunRange;

                var camMapPos = _transform.ToMapCoordinates(xform.Coordinates);
                if (!mapPos.InRange(camMapPos, maxRange))
                    continue;

                if (shooter.HasValue)
                {
                    // require line-of-sight on unsuppressed gunshots
                    if (!_interaction.InRangeUnobstructed(camUid, shooter.Value, maxRange))
                        continue;
                }

                // Cooldown check
                if (TryComp<OverwatchConsoleComponent>(camUid, out var _))
                {
                    // camera also a console? ignore
                }

                // Build alert
                var alertType = "Стрельба";
                var sector = surv.CameraId ?? "Неизвестный сектор";

                AddAlert(camUid, alertType, sector, true);
            }
        }

        private void AddAlert(EntityUid cameraUid, string type, string sector, bool playSound)
        {
            var timeStr = _gameTicker.RoundDuration().ToString(@"hh\:mm\:ss");
            var transform = Transform(cameraUid);
            var gridPos = _transform.GetGridOrMapTilePosition(cameraUid, transform);
            var sectorWithCoords = $"({gridPos.X}, {gridPos.Y}) {sector}";

            // update every console on station
            var consoles = EntityQueryEnumerator<OverwatchConsoleComponent>();
            while (consoles.MoveNext(out var uid, out var comp))
            {
                // cooldown per camera
                var now = (float) _timing.CurTime.TotalSeconds;
                if (comp.LastAlertTime.TryGetValue(cameraUid, out var last) && now - last < GunshotCooldown)
                {
                    var netCam = EntityManager.GetNetEntity(cameraUid);
                    // update existing alert timestamp but do not create new
                    foreach (var a in comp.ActiveAlerts.Values)
                    {
                        if (a.CameraUid == netCam)
                        {
                            a.TimeStr = timeStr;
                            a.Sector = sectorWithCoords;
                            break;
                        }
                    }
                }
                else
                {
                    var id = comp.NextAlertId++;
                    // derive camera name from surveillance component if available
                    var camName = sector;
                    comp.ActiveAlerts[id] = new OverwatchAlertData(id, type, sectorWithCoords, camName, timeStr, EntityManager.GetNetEntity(cameraUid));
                    comp.LastAlertTime[cameraUid] = now;

                    // play alarm sound at the console if high priority
                    if (playSound)
                        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Effects/alert.ogg"), Filter.Pvs(uid), true);
                }

                UpdateConsoleUi(uid, comp);
            }
        }
    }
}
