using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Robust.Server.Audio;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects
{
    public sealed class WormholeAnomalySystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            SubscribeLocalEvent<WormholeAnomalyComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, WormholeAnomalyComponent component, ComponentInit args)
        {
            StartPulseTimer(uid, component);
        }

        private void StartPulseTimer(EntityUid uid, WormholeAnomalyComponent component)
        {
            Timer.Spawn((int)(component.PulseInterval * 1000), () => OnPulse(uid, component));
        }

        private void OnPulse(EntityUid uid, WormholeAnomalyComponent component)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            if (!xformQuery.TryGetComponent(uid, out var xform))
                return;

            var range = component.MaxShuffleRadius;
            var newPosition = _random.NextVector2(range);

            _xform.SetWorldPosition(uid, newPosition);
            _audio.PlayPvs(component.TeleportSound, uid);

            StartPulseTimer(uid, component);
        }
    }
}
