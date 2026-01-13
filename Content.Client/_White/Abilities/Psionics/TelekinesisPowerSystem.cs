using Content.Shared._White.Abilities.Psionics;
using Content.Shared._White.Psionics.Abilities;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._White.Abilities.Psionics;

public sealed class TelekinesisPowerSystem : SharedTelekinesisPowerSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityCoordinates _lastSentCoords;
    private TimeSpan _timeSinceLastSend;
    private const float SendInterval = 1;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        _timeSinceLastSend += TimeSpan.FromSeconds(frameTime);

        var player = _player.LocalEntity;

        if (player == null || !TryComp<TelekinesisPowerComponent>(player, out var power))
            return;

        if (power.TetheredEntity == null)
            return;

        var mousePos = _input.MouseScreenPosition;
        var mouseWorldPos = _eyeManager.PixelToMap(mousePos);

        if (mouseWorldPos.MapId == MapId.Nullspace)
            return;

        EntityCoordinates coords;

        if (_mapManager.TryFindGridAt(mouseWorldPos, out var gridUid, out _))
            coords = _transform.ToCoordinates(gridUid, mouseWorldPos);
        else
        {
            coords = _transform.ToCoordinates(mouseWorldPos);
        }

        if (_timeSinceLastSend.TotalSeconds >= SendInterval && coords != _lastSentCoords)
        {
            RaisePredictiveEvent(new RequestTelekinesisMoveEvent()
            {
                Coordinates = GetNetCoordinates(coords)
            });
            _lastSentCoords = coords;
            _timeSinceLastSend = TimeSpan.Zero;
        }
    }
}
