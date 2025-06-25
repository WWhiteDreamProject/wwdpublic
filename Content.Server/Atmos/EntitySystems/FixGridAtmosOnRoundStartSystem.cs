using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Atmos.Components;
using Robust.Shared.Console;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Executes the fixgridatmos console command for any entity with
/// <see cref="FixGridAtmosOnRoundStartComponent"/> when a round starts.
/// </summary>
public sealed partial class FixGridAtmosOnRoundStartSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        var query = AllEntityQuery<FixGridAtmosOnRoundStartComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid is not { } grid)
                continue;

            if (!_entities.TryGetNetEntity(grid, out var netGrid))
                continue;

            _consoleHost.ExecuteCommand($"fixgridatmos {netGrid.Value.Id}");
        }
    }
}
