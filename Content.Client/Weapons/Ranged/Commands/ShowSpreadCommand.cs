using Content.Client.Weapons.Ranged.Systems;
using Robust.Shared.Console;
using Robust.Shared.Reflection;
using System.Linq;
using static Content.Client.Weapons.Ranged.Systems.GunSystem;

namespace Content.Client.Weapons.Ranged;

public sealed class ShowSpreadCommand : IConsoleCommand
{
    public string Command => "showgunspread";
    public string Description => $"Switches gun spread overlay between normal and debug."; // wwdp edit
    // WWDP EDIT START
    public string Help => "Shows all the spread related values for currently held gun (or for the localEntity's gunComp, if no gun is held and the component is present.)"; // wwdp edit

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GunSystem>();

        system.SpreadOverlay = system.SpreadOverlay switch
        {
            GunSpreadOverlayEnum.Off => GunSpreadOverlayEnum.Normal,
            GunSpreadOverlayEnum.Normal => GunSpreadOverlayEnum.Debug,
            GunSpreadOverlayEnum.Debug => GunSpreadOverlayEnum.Off,
            _ => throw new ArgumentOutOfRangeException()
        };

        shell.WriteLine($"Set spread overlay to \"{system.SpreadOverlay}\".");
    }
}

// WWDP EDIT END
