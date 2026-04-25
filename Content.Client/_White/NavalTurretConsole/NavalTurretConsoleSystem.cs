using System.Numerics;
using Content.Client._White.NavalTurretConsole.UI;
using Content.Shared._White.NavalTurretControl;
using Content.Shared._White.NavalTurretControl.BUIStates;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.DeviceLinking;
using Content.Shared.MouseRotator;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._White.NavalTurretControl;

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

public sealed partial class NavalTurretControlSystem : SharedNavalTurretConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NavalTurretConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var consoleUid, out var consoleComp, out var uiComp))
        {
            ProcessInput(consoleUid, consoleComp, uiComp);
            ProcessConsole(consoleComp, frameTime);
        }
    }

    private void ProcessInput(EntityUid consoleUid, NavalTurretConsoleComponent consoleComp, UserInterfaceComponent uiComp)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_ui.TryGetOpenUi<NavalTurretConsoleBoundUserInterface>((consoleUid, uiComp), NavalTurretConsoleUiKey.Key, out var bui))
            return;

        if (bui.AimDirection  == consoleComp.CurrentAimDirection)
            return;

        _ui.SendPredictedUiMessage(bui, new NavalTurretConsoleUpdateAimDirectionMessage(bui.AimDirection));
    }
}