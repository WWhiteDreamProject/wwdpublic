using System;
using System.Linq;
using System.Text;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Content.Shared.Administration;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class PoshelNahuiCommand : IConsoleCommand
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public string Command => "poshelnahui";
        public string Description => "Shlet nahui blya.";
        public string Help => "Usage: poshelnahui che neponyatno";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                var player = shell.Player;
                var toPoshelNahuiPlayer = player ?? _players.Sessions.FirstOrDefault();
                if (toPoshelNahuiPlayer == null)
                {
                    shell.WriteLine("You need to provide a player to poslat nahui.");
                    return;
                }
                shell.WriteLine($"You need to provide a player to poslat nahui. Try running 'poshelnahui {toPoshelNahuiPlayer.Name}' as an example.");
                return;
            }

            var name = args[0];

            if (_players.TryGetSessionByUsername(name, out var target))
            {
                string reason;
                if (args.Length >= 2)
                    reason = $"Poslan nahui by console: {string.Join(' ', args[1..])}";
                else
                    reason = "Poslan nahui by console";

                _netManager.DisconnectChannel(target.Channel, reason);
            }
        }
    }
}
