using Content.Server._White.Body.Systems;
using Content.Server._White.Commands;
using Content.Server.Administration;
using Content.Shared._White.Body.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._White.Body.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class CreateOrganSlotCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "createorganslot";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var organType = OrganType.None;
        if (args.Length is < 2 or > 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netUid))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        if (args.Length == 3 && !Enum.TryParse(args[2], out organType))
        {
            shell.WriteError(Loc.GetString("shell-argument-organ-type-invalid", ("index", args[2])));
            return;
        }

        var uid = _entManager.GetEntity(netUid);

        var bodySystem = _entManager.System<BodySystem>();

        if (!bodySystem.TryCreateOrganSlot(uid, args[1], organType))
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-fail"));
            return;
        }

        shell.WriteLine(Loc.GetString("shell-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-entity-hint"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-string-hint"));

        if (args.Length == 3)
            return CompletionResult.FromHintOptions(WhiteCompletionHelper.Emuns(typeof(OrganType)), Loc.GetString("shell-argument-organ-type-optional-hint"));

        return CompletionResult.Empty;
    }
}
