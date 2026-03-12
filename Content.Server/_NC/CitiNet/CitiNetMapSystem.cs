using System;
using System.Numerics;
using Content.Shared._NC.CitiNet;
using Robust.Shared.Timing;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server._NC.CitiNet;

/// <summary>
/// Core system for managing dynamic events on the CitiNet Map.
/// Handles manual pings, trackers, and temporary SOS signals.
/// </summary>
public sealed class CitiNetMapSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<TransientPing> _transientPings = new();
    private const float PingDuration = 5.0f; 

    public override void Initialize()
    {
        base.Initialize();
        // Automatic gunshot tracking removed as per request.
    }

    public void AddPing(Vector2 localPos, Color color, float radius, CitiNetPingType type)
    {
        _transientPings.Add(new TransientPing
        {
            Position = localPos,
            Color = color,
            Radius = radius,
            Type = type,
            ExpireTime = _timing.CurTime + TimeSpan.FromSeconds(PingDuration)
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_transientPings.Count > 0)
        {
            _transientPings.RemoveAll(p => _timing.CurTime > p.ExpireTime);
        }
    }

    public List<CitiNetMapPingData> GetActivePings(EntityUid gridUid)
    {
        // Currently returns all transient pings. 
        return _transientPings.Select(p => new CitiNetMapPingData(p.Position, p.Color, p.Radius, p.Type)).ToList();
    }

    private struct TransientPing
    {
        public Vector2 Position;
        public Color Color;
        public float Radius;
        public CitiNetPingType Type;
        public TimeSpan ExpireTime;
    }
}
