using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._War.StructureHeatExchange;

// [AdminCommand(AdminFlags.Admin)]
// public sealed class GlobalGoidaCommand : IConsoleCommand
// {
//     [Dependency] private readonly IEntitySystemManager _entSysMan = default!;
//
//     public string Command => "globalgoida";
//     public string Description => "Enables heat exchange";
//     public string Help => "";
//
//     public void Execute(IConsoleShell shell, string argStr, string[] args)
//     {
//         var heat = _entSysMan.GetEntitySystem<StructureHeatExchangeSystem>();
//
//         heat.GlobalGoida();
//     }
// }

[AdminCommand(AdminFlags.Admin)]
public sealed class GoidaCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entSysMan = default!;
    [Dependency] private readonly ILogManager _logMan = default!;

    public string Command => "goida";
    public string Description => "Enables heat exchange";
    public string Help => "goida -> Переключает теплообмен везде. goida 10 -> Меняет скорость теплообмена. ДЕФОЛТ это 5 (25.05.2025)";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var heat = _entSysMan.GetEntitySystem<StructureHeatExchangeSystem>();
        var sawmill = _logMan.GetSawmill("HeatExchange");
        var playerName = shell.Player?.AttachedEntity.ToString();
        var zalupa = shell.Player?.Name;

        if (string.IsNullOrEmpty(playerName)
            && !string.IsNullOrEmpty(zalupa))
        {
            playerName = zalupa;
        }
        else
        {
            playerName = "Server";
        }

        if (args.Length == 0)
        {
            heat.Enabled = !heat.Enabled;
            shell.WriteLine(heat.Enabled ? "Heat exchange enabled!" : "Heat exchange disabled!");
            sawmill.Warning((heat.Enabled ? "Heat exchange enabled by " : "Heat exchange disabled by ") + playerName + '.');
            return;
        }

        if (args.Length == 1 && float.TryParse(args[0], out var coeff))
        {
            var lastM = heat.Multiplier;
            heat.Multiplier = coeff;
            shell.WriteLine($"Heat exchange set up to: {heat.Multiplier}. Last time exchange was: {lastM}.");
            sawmill.Warning($"Heat exchange set up to: {heat.Multiplier}. Last time exchange was: {lastM}. By {playerName}");
        }
    }
}
