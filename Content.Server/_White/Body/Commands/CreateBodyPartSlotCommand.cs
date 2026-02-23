using Content.Server._White.Body.Systems;
using Content.Server._White.Commands;
using Content.Server.Administration;
using Content.Shared._White.Body.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._White.Body.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class CreateBodyPartCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "createbodypartslot";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var bodyPartType = BodyPartType.None;
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

        if (args.Length == 3 && !Enum.TryParse(args[2], out bodyPartType))
        {
            shell.WriteError(Loc.GetString("shell-argument-body-part-type-invalid", ("index", args[2])));
            return;
        }

        var uid = _entManager.GetEntity(netUid);

        var bodySystem = _entManager.System<BodySystem>();

        if (!bodySystem.TryCreateBodyPartSlot(uid, args[1], bodyPartType))
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
            return CompletionResult.FromHintOptions(WhiteCompletionHelper.Emuns(typeof(BodyPartType)), Loc.GetString("shell-argument-body-part-type-optional-hint"));

        return CompletionResult.Empty;
    }
}
