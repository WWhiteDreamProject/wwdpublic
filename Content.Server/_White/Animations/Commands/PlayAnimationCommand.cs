using Content.Server._White.Animations.Systems;
using Content.Server.Administration;
using Content.Shared._White.Animations.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._White.Animations.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class PlayAnimationCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "playanimation";

    public override void Execute(IConsoleShell shell, string arg, string[] args)
    {
        if (args.Length == 1)
        {
            var entity = shell.Player?.AttachedEntity;
            if (!entity.HasValue)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                shell.WriteLine(Help);
                return;
            }

            _entityManager.System<WhiteAnimationPlayerSystem>().Play(entity.Value, args[0]);
            return;
        }

        if (args.Length == 2)
        {
            if (!NetEntity.TryParse(args[1], out var netEntity))
            {
                shell.WriteError(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            var entity = _entityManager.GetEntity(netEntity);
            _entityManager.System<WhiteAnimationPlayerSystem>().Play(entity, args[0]);
            return;
        }

        shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
        shell.WriteLine(Help);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<AnimationPrototype>(), Loc.GetString("shell-argument-animation-id-hint"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("shell-argument-entity-optional-hint"));

        return CompletionResult.Empty;
    }
}
