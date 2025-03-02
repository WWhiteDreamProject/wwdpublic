using Content.Client.Weapons.Ranged.Systems;
using Robust.Shared.Console;
using Robust.Shared.Reflection;
using System.Linq;
using static Content.Client.Weapons.Ranged.Systems.GunSystem;

namespace Content.Client.Weapons.Ranged;

public sealed class ShowSpreadCommand : IConsoleCommand
{
    public string Command => "showgunspread";
    public string Description => $"Shows gun spread overlay for debugging";
    // WWDP EDIT START
    // I REGRET DOING ENUMS HERE
    public string Help => $"{Command} off/partial/full";

    readonly Dictionary<string, GunSpreadOverlayEnum> dick = new() {
        {"off", GunSpreadOverlayEnum.Off},
        {"partial", GunSpreadOverlayEnum.Partial },
        {"full", GunSpreadOverlayEnum.Full}
    };

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length <= 1)
            return CompletionResult.FromOptions(dick.Keys);
        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GunSystem>();

        if (args.Length != 1 ||
            !dick.TryGetValue(args[0].ToLower(), out var option))
        {
            shell.WriteLine(Help);
            return;
        }

        if (system.SpreadOverlay == option)
        {
            shell.WriteLine($"Spread overlay already set to \"{system.SpreadOverlay}\".");
        }
        else {
            system.SpreadOverlay = option;
            shell.WriteLine($"Set spread overlay to \"{system.SpreadOverlay}\".");
        }
    }
}

public sealed class ShowFullSpreadCommand : IConsoleCommand
{
    public string Command => "showgunspreadfull";
    public string Description => $"Shows gun spread overlay for debugging";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GunSystem>();
        if (system.SpreadOverlay == GunSystem.GunSpreadOverlayEnum.Full)
            system.SpreadOverlay = GunSystem.GunSpreadOverlayEnum.Off;
        else
            system.SpreadOverlay = GunSystem.GunSpreadOverlayEnum.Full;

        shell.WriteLine($"Set spread overlay to \"{system.SpreadOverlay}\".");
    }
}
// WWDP EDIT END
