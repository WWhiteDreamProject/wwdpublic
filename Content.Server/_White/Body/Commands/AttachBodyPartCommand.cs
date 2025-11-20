using Content.Server._White.Body.Systems;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._White.Body.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AttachBodyPartCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "attachbodypart";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var parentNetUid)
            || !NetEntity.TryParse(args[1], out var bodyPartNetUid))
        {
            shell.WriteError(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        var parentUid = _entManager.GetEntity(parentNetUid);
        var bodyPartUid = _entManager.GetEntity(bodyPartNetUid);

        var bodySystem = _entManager.System<BodySystem>();

        if (!bodySystem.TryAttachBodyPart(parentUid, bodyPartUid, args[2]))
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
            return CompletionResult.FromHint(Loc.GetString("shell-argument-entity-hint"));

        if (args.Length == 3)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-string-hint"));

        return CompletionResult.Empty;
    }
}
