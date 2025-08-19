using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared._White.Bark;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using BarkComponent = Content.Shared._White.Bark.Components.BarkComponent;
using BarkSourceComponent = Content.Shared._White.Bark.Components.BarkSourceComponent;


namespace Content.Server._White.Bark;

public sealed class BarkSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly AudioSystem _sharedAudio = default!;

    private static readonly char[] LongPauseChars = ['.', ',', '?', '!',];
    private static readonly char[] SkipChars = [' ', '\n', '\r', '\t',];
    private static readonly char[] Soglasnoy = ['Б', 'В', 'Г', 'Д', 'Ж', 'З', 'Й', 'К', 'Л', 'М', 'Н', 'П', 'Р', 'С', 'Т', 'Ф', 'Х', 'Ц', 'Ч', 'Ш', 'Щ', ];

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<BarkComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(Entity<BarkComponent> ent, ref EntitySpokeEvent args)
    {
        Bark(ent, args.Message);
    }

    public void Bark(Entity<BarkComponent> entity, string text)
    {
        var barkList = new List<BarkData>();
        for (var i = 0; i < text.Length; i++)
        {
            var currChar = text[i];

            var currBark = new BarkData(entity.Comp.PitchAverage, entity.Comp.VolumeAverage, entity.Comp.PauseAverage);

            if (SkipChars.Contains(currChar))
                currBark.Enabled = false;

            if (LongPauseChars.Contains(currChar))
            {
                currBark.Pause += 0.5f;
                currBark.Enabled = false;
            }

            //if (char.IsUpper(currChar))
            //    currBark.Pitch += 0.4f;

            if (Soglasnoy.Contains(currChar))
            {
                currBark.Pitch -= 0.1f;
                currBark.Volume -= 0.1f;
            }

            currBark.Pitch += Random.Shared.NextFloat(-entity.Comp.PitchVariance, entity.Comp.PitchVariance);

            barkList.Add(currBark);
        }

        Bark(entity, barkList);
    }


    public void Bark(Entity<BarkComponent> entity, List<BarkData> barks)
    {
        if (TryComp<BarkSourceComponent>(entity, out var sourceComponent))
        {
            RemComp(entity, sourceComponent);
        }

        sourceComponent = AddComp<BarkSourceComponent>(entity);
        sourceComponent.Barks = new(barks);
        sourceComponent.Action = new SoundBarkAction()
        {
            SoundSpecifier = _sharedAudio.ResolveSound(entity.Comp.BarkSound)
        };
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BarkSourceComponent>();
        while (query.MoveNext(out var uid, out var barkSource))
        {
            barkSource.BarkTime += frameTime;

            if (barkSource.CurrentBark is null)
            {
                if (!barkSource.Barks.TryDequeue(out var barkData))
                {
                    RemComp(uid, barkSource);
                    continue;
                }

                barkSource.CurrentBark = barkData;
            }

            if (barkSource.CurrentBark.Value.Pause <= barkSource.BarkTime)
            {
                barkSource.BarkTime = 0;
                barkSource.Action.Act(
                    _entitySystemManager.DependencyCollection,
                    new(uid, barkSource),
                    barkSource.CurrentBark.Value
                    );
                barkSource.CurrentBark = null;
            }
        }
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
    }
}
