using System.Numerics;
using Content.Client._White.RemoteControlConsole.UI;
using Content.Client.Gameplay;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.RemoteControl.BUIStates;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.DeviceLinking;
using Content.Shared.MouseRotator;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._White.RemoteControl;

// TODO: Consider moving this functionality to a separate entitysystem
//       that is generalized to be used by any UI
//       something like this, perhaps:
//       public void SendCachedPredictedUiMessage(key, bui, msg) {
//          _cachedMsg[key] = (bui, msg);
//       }
//       public override void Update(float frameTime){
//          if(!_timing.IsFirstTimePredicted)
//              return;
//          foreach(var (key, (bui, msg)) in _cachedMsg.key)
//              _ui.SendPredictedUiMessage(bui, msg); // maybe also run a IsUiOpen() check here? 
//          _cachedMsg.Clear();
//       }

public sealed partial class RemoteControlSystem : SharedRemoteControlSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IStateManager _state = default!;

    protected override void ProcessConsole(EntityUid consoleUid, RemoteControlConsoleComponent consoleComp, float frameTime)
    {
        ProcessInput(consoleUid, consoleComp);
        base.ProcessConsole(consoleUid, consoleComp, frameTime);
    }
    private void ProcessInput(EntityUid consoleUid, RemoteControlConsoleComponent consoleComp)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_ui.TryGetOpenUi<RemoteControlConsoleBoundUserInterface>(consoleUid, RemoteControlConsoleUiKey.Key, out var bui))
            return;

        if (bui.AimDirection == consoleComp.CurrentAimDirection)
            return;


        NetEntity? target = null;
        if (bui.CameraAimpoint is MapCoordinates aimpoint &&
            _state.CurrentState is GameplayState state)
        {
            if(!TryComp<EyeComponent>(consoleComp.CurrentTurret, out var eyeComp))
                DebugTools.Assert("Bui passed an aimpoint without an eye component on active turret. Possible invalid/broken state?");
            target = GetNetEntity(state.GetClickedEntity(aimpoint, eyeComp.Eye));
        }

        _ui.SendPredictedUiMessage(bui, new RemoteControlConsoleUpdateAimDirectionMessage(bui.AimDirection, target));
    }
}