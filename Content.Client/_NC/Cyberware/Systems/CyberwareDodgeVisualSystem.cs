using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Client._NC.Cyberware.Systems;

public sealed class CyberwareDodgeVisualSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CyberwareDodgeEvent>(OnDodgeEvent);
    }

    private void OnDodgeEvent(CyberwareDodgeEvent args)
    {
        if (!TryGetEntity(args.Target, out var uid))
            return;

        var visual = EnsureComp<CyberwareDodgeVisualComponent>(uid.Value);
        
        // Выбираем случайное направление: влево (-1) или вправо (1) на 70 см
        var xOffset = _random.Prob(0.5f) ? -0.7f : 0.7f;
        visual.Direction = new Vector2(xOffset, 0);
        visual.Accumulator = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CyberwareDodgeVisualComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var visual, out var sprite))
        {
            visual.Accumulator += frameTime;

            if (visual.Accumulator >= visual.Lifetime)
            {
                // Время вышло — возвращаем спрайт в центр и удаляем локальный компонент
                sprite.Offset = Vector2.Zero;
                RemCompDeferred<CyberwareDodgeVisualComponent>(uid);
                continue;
            }

            // Вычисляем смещение: синусоида дает плавный рывок туда-обратно
            // sin(pi * progress) идет от 0 до 1 и обратно в 0.
            var progress = visual.Accumulator / visual.Lifetime;
            var offsetMultiplier = MathF.Sin(MathF.PI * progress);
            
            sprite.Offset = visual.Direction * offsetMultiplier;
        }
    }
}
