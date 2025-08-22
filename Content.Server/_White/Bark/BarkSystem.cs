using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared._White.Bark;
using Content.Shared._White.Bark.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using BarkComponent = Content.Shared._White.Bark.Components.BarkComponent;


namespace Content.Server._White.Bark;

public sealed class BarkSystem : SharedBarkSystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<BarkComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(Entity<BarkComponent> ent, ref EntitySpokeEvent args)
    {
        Bark(ent, args.Message, args.IsWhisper);
    }

    public override void Bark(Entity<BarkComponent> entity, List<BarkData> barks)
    {
        var grid = _transformSystem.GetGrid(entity.Owner);
        var mapPos = _transformSystem.GetMapCoordinates(entity.Owner);
        if(grid is null)
            return;

        RaiseNetworkEvent(
            new EntityBarkEvent(GetNetEntity(entity), barks),
            Filter
                .BroadcastGrid(grid.Value)
                .AddInRange(mapPos,16));

    }
}


public sealed class AddBarkCommand : IConsoleCommand
{
    public string Command => "addbark";
    public string Description => "add bark to self";
    public string Help => Command;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!entMan.TryParseNetEntity(args[0], out var attachedEnt))
        {
            shell.WriteError($"Could not find attached entity " + args[0]);
            return;
        }

        var comp = entMan.AddComponent<BarkComponent>(attachedEnt.Value);
        comp.BarkSound = new SoundCollectionSpecifier("BarkMeow");
        entMan.Dirty(attachedEnt.Value, comp);
    }
}
