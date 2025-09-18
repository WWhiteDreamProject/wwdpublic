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
public sealed class SetGhostCommand : IConsoleCommand
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefMan = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "setghost";
    public string Description => Loc.GetString("setghost-command-description");
    public string Help => Loc.GetString("setghost-command-help-text");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not ICommonSession player)
        {
            shell.WriteLine(Loc.GetString("setghost-command-no-session"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var selectedProto = args[0];

        if (!_proto.TryIndex<CustomGhostPrototype>(selectedProto, out var proto))
        {
            shell.WriteLine(Loc.GetString("setghost-command-invalid-ghost-id"));
            return;
        }

        if (proto.Ckey is string ckey && player.Name.ToLower() != ckey.ToLower())
        {
            shell.WriteLine(Loc.GetString("setghost-command-exclusive-ghost"));
            return;
        }

        if (proto.PlaytimeHours is not null)
        {
            var trackers = proto.PlaytimeHours.Keys;
            var playtimes = await _db.GetPlayTimes(player.UserId);
            StringBuilder rejects = new();

            foreach(var tracker in trackers)
            {
                float hoursPlayed = (float)_playTimeTracking.GetPlayTimeForTracker(player, tracker).TotalHours;
                float hoursRequired = proto.PlaytimeHours[tracker];

                if (hoursPlayed < hoursRequired)                                                          // does not respect admin authority
                    rejects.AppendLine(Loc.GetString("setghost-command-insufficient-playtime-partial",   // drip or drown, jannie man
                        ("tracker", tracker),
                        ("required", hoursRequired),
                        ("playtime", hoursPlayed))
                    );
            }

            if(rejects.Length != 0) // dumb and ugly but it works
            {
                shell.WriteLine(Loc.GetString("setghost-command-insufficient-playtime"));
                shell.WriteLine(rejects.ToString());
                return;
            }
        }

        await _db.SaveGhostTypeAsync(player.UserId, selectedProto);
        var prefs = _prefMan.GetPreferences(player.UserId);
        prefs.CustomGhost = selectedProto;
        shell.WriteLine(Loc.GetString("setghost-command-saved"));
    }
}
