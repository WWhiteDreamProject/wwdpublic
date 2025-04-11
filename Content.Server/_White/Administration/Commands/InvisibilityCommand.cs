using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Robust.Shared.Console;

namespace Content.Server._White.Administration.Commands;

[AdminCommand(AdminFlags.Stealth)]
public sealed class InvisibilityCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "invisibility";
    public override string Description => LocalizationManager.GetString("invisibility-description");
    public override string Help => "Usage: invisibility";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entity = shell.Player?.AttachedEntity;
        if (entity == null)
        {
            shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (!_entManager.TryGetComponent<VisibilityComponent>(entity, out var visibilityComponent))
        {
            shell.WriteError(LocalizationManager.GetString("invisibility-target-player-is-not-a-ghost"));
            return;
        }

        var visibility = _entManager.System<VisibilitySystem>();

        visibility.AddLayer((entity.Value, visibilityComponent), (int) VisibilityFlags.AGhost, false);
        visibility.RemoveLayer((entity.Value, visibilityComponent), (int) VisibilityFlags.Ghost, false);
        visibility.RefreshVisibility(entity.Value, visibilityComponent);
    }
}
