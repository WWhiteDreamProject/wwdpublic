using Content.Server._White.Commands;
using Content.Server.Administration;
using Content.Server.Hands.Systems;
using Content.Shared.Administration;
using Content.Shared.Hands.Components;
using Robust.Shared.Console;

namespace Content.Server._White.Hands.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddHandCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "addhand";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid entity = default;
        var handName = "hand";
        var handLocation = HandLocation.Middle;

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

        if (args.Length >= 3 && !Enum.TryParse(args[2], out handLocation))
        {
            shell.WriteError(Loc.GetString("shell-argument-hand-location-invalid", ("index", args[2])));
            return;
        }

        _entManager.System<HandsSystem>().AddHand(entity, handName, handLocation);

        shell.WriteLine(Loc.GetString("shell-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-entity-optional-hint"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-string-optional-hint"));

        if (args.Length == 3)
            return CompletionResult.FromHintOptions(WhiteCompletionHelper.Emuns(typeof(HandLocation)), Loc.GetString("shell-argument-hand-location-optional-hint"));

        return CompletionResult.Empty;
    }
}
