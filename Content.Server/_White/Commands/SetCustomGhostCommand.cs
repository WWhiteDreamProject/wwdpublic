using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared._White.CustomGhostSystem;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Commands;

[AnyCommand]
public sealed class SetCustomGhostCommand : IConsoleCommand
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefMan = default!;

    public string Command => "setcustomghost";
    public string Description => Loc.GetString("setcustomghost-command-description");
    public string Help => Loc.GetString("setcustomghost-command-help-text");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not ICommonSession player)
        {
            shell.WriteLine(Loc.GetString("setcustomghost-command-no-session"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var protoId = args[0];

        if (!_proto.TryIndex<CustomGhostPrototype>(protoId, out var proto))
        {
            shell.WriteLine(Loc.GetString("setcustomghost-command-invalid-ghost-id"));
            return;
        }

        if (!proto.CanUse(player, out var failReason))
        {
            shell.WriteLine(failReason);
            return;
        }

        await _db.SaveGhostTypeAsync(player.UserId, protoId);
        var prefs = _prefMan.GetPreferences(player.UserId);
        prefs.CustomGhost = protoId;
        shell.WriteLine(Loc.GetString("setcustomghost-command-saved"));
    }
}
