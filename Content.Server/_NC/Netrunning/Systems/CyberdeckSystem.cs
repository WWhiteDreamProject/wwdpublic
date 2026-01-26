using Content.Shared._NC.Netrunning.Components;
using Content.Server.Power.Components;
using Robust.Shared.Timing;

namespace Content.Server._NC.Netrunning.Systems;

public sealed class CyberdeckSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // SubscribeLocalEvent<CyberdeckComponent, ...>(...);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CyberdeckComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var deck, out var battery))
        {
            if (deck.CurrentRam >= deck.MaxRam)
                continue;

            // Only regenerate if battery has charge
            if (battery.CurrentCharge <= 0)
                continue;

            deck.RecoveryAccumulator += frameTime;
            if (deck.RecoveryAccumulator >= 1.0f)
            {
                deck.RecoveryAccumulator -= 1.0f;
                // Recover RAM based on speed
                int amount = (int) System.Math.Floor(deck.RecoverySpeed);
                // Handle fractional speed better? For now simple integer steps per second.
                // Or: acc += frameTime * speed.

                // Let's change logic: Accumulator reaches 1/Speed then adds 1 RAM.
                // But deck.RecoverySpeed is "RAM per second".
                // So expected gain is frameTime * RecoverySpeed.
                // Since RAM is int, we accumulate "partial RAM" or just tick whole numbers.

                // Better approach:
                // Accumulate tokens.

                // Revert to simple 1 sec interval adds Speed amount.
                deck.CurrentRam = System.Math.Min(deck.CurrentRam + amount, deck.MaxRam);

                // Consume a bit of power for netrunning standby?
                // Optional.

                Dirty(uid, deck);
            }
        }
    }

    public bool TryUseRam(EntityUid uid, int cost, CyberdeckComponent? deck = null)
    {
        if (!Resolve(uid, ref deck))
            return false;

        if (deck.CurrentRam >= cost)
        {
            deck.CurrentRam -= cost;
            Dirty(uid, deck);
            return true;
        }

        return false;
    }
}
