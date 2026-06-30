using Content.Server.Stunnable;
using Content.Server.Temperature.Systems;
using Content.Shared._White.Actions.Events;

namespace Content.Server._White.Abilities.Invoker;

public sealed class ColdSnapActionSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temp = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColdSnapActionEvent>(OnUsed);
    }

    private void OnUsed(ColdSnapActionEvent args)
    {
        _temp.ForceChangeTemperature(args.Target, -273f);
        _stun.TryStun(args.Target, TimeSpan.FromSeconds(2f), false);
        _stun.TrySlowdown(args.Target, TimeSpan.FromSeconds(10f), false);

        args.Handled = true;
    }
}
