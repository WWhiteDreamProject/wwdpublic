using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared._NC.Cyberware.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class EyeCursorOffsetSystem : SharedEyeCursorOffsetSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static bool _toggled;
    private Vector2 _localOffset = Vector2.Zero;
    
    private TimeSpan _lastSync = TimeSpan.Zero;
    private readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(0.1);

    private const float Deadzone = 0.2f; 
    private const float DriftSpeed = 15f; 

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeComponent, GetEyeOffsetEvent>(OnLocalPlayerGetEyeOffset);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.LookUp, new EyeOffsetInputCmdHandler())
            .Register<EyeCursorOffsetSystem>();
    }

    protected override bool IsLocalPlayer(EntityUid uid) => _player.LocalEntity == uid;

    private void OnLocalPlayerGetEyeOffset(EntityUid uid, EyeComponent eye, ref GetEyeOffsetEvent args)
    {
        if (uid != _player.LocalEntity)
            return;

        if (!TryComp<EyeCursorOffsetComponent>(uid, out var comp))
        {
            if (TryComp<CyberwareComponent>(uid, out var cyberware))
            {
                foreach (var implant in cyberware.InstalledImplants.Values)
                {
                    if (TryComp<CyberwareMicroOpticsComponent>(implant, out var optics))
                    {
                        comp = EnsureComp<EyeCursorOffsetComponent>(uid);
                        comp.MaxOffset = optics.MaxOffset;
                        comp.PvsIncrease = optics.PvsIncrease;
                        break;
                    }
                }
            }
        }

        if (comp == null)
            return;

        UpdateDrift(uid, comp);
        args.Offset += _localOffset;
    }

    private void UpdateDrift(EntityUid uid, EyeCursorOffsetComponent component)
    {
        var frameTime = (float)_timing.FrameTime.TotalSeconds;

        bool active = _toggled;
        if (TryComp<Content.Shared.CombatMode.CombatModeComponent>(uid, out var combatMode))
            active &= combatMode.IsInCombatMode;

        if (!active)
        {
            if (_localOffset != Vector2.Zero)
            {
                var step = DriftSpeed * 2 * frameTime; 
                if (_localOffset.Length() <= step)
                    _localOffset = Vector2.Zero;
                else
                    _localOffset -= _localOffset.Normalized() * step;

                SyncOffset(_localOffset);
            }
            return;
        }

        var mousePos = _inputManager.MouseScreenPosition;
        if (mousePos.Window == Robust.Shared.Map.WindowId.Invalid)
            return;

        var screenSize = _clyde.MainWindow.Size;
        var center = new Vector2(screenSize.X / 2f, screenSize.Y / 2f);
        var mouseVec = new Vector2(mousePos.X - center.X, mousePos.Y - center.Y);
        var maxDim = MathF.Min(center.X, center.Y);
        var normalizedMouse = mouseVec / maxDim;

        if (normalizedMouse.Length() > Deadzone)
        {
            var eyeRotation = _eyeManager.CurrentEye.Rotation;
            var driftDir = Vector2.Transform(normalizedMouse, System.Numerics.Quaternion.CreateFromAxisAngle(-System.Numerics.Vector3.UnitZ, (float)eyeRotation.Opposite().Theta));
            driftDir.X = -driftDir.X; 

            _localOffset += driftDir.Normalized() * DriftSpeed * frameTime;

            if (_localOffset.Length() > component.MaxOffset)
                _localOffset = _localOffset.Normalized() * component.MaxOffset;

            if (_timing.CurTime > _lastSync + _syncInterval)
            {
                SyncOffset(_localOffset);
                _lastSync = _timing.CurTime;
            }
        }
    }

    private void SyncOffset(Vector2 offset)
    {
        RaiseNetworkEvent(new RequestEyeCursorOffsetEvent(offset));
    }

    private sealed class EyeOffsetInputCmdHandler : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entManager, Robust.Shared.Player.ICommonSession? session, IFullInputCmdMessage message)
        {
            _toggled = message.State == BoundKeyState.Down;
            return false;
        }
    }
}
