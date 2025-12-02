using Content.Server.Administration;
using Content.Server.Hands.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._White.Hands.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class RemoveHandCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "removehand";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid entity = default;
        var handName = "hand";

        if (args.Length == 0)
        {
            if (shell.Player?.AttachedEntity == null)
            {
                shell.WriteError(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            entity = shell.Player.AttachedEntity.Value;
        }

        if (args.Length >= 1)
        {
            if (!NetEntity.TryParse(args[0], out var netEntity))
            {
                shell.WriteError(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            entity = _entManager.GetEntity(netEntity);
        }

        if (args.Length >= 2)
            handName = args[1];

        _entManager.System<HandsSystem>().RemoveHand(entity, handName);

        shell.WriteLine(Loc.GetString("shell-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-entity-optional-hint"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-string-optional-hint"));

        return CompletionResult.Empty;
    }
}
