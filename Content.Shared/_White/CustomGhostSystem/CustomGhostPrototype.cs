using Content.Shared.Ghost;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared._White.CustomGhostSystem;

[Prototype("customGhost")]
public sealed class CustomGhostPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [ViewVariables]
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [ViewVariables]
    [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<CustomGhostPrototype>))]
    public string[]? Parents { get; }

    [DataField, AlwaysPushInheritance]
    public string Category { get; private set; } = "Misc";

    [DataField, AlwaysPushInheritance]
    public List<CustomGhostRestriction>? Restrictions { get; private set; }

    [DataField("proto", required: true), AlwaysPushInheritance]
    public EntProtoId<GhostComponent> GhostEntityPrototype { get; private set; } = default!;

    [DataField("name"), AlwaysPushInheritance]
    public string? Name { get; private set; }

    [DataField("desc"), AlwaysPushInheritance]
    public string? Description { get; private set; }
}


//public static class CustomGhostAvailabilityCheck
//{
//    private static ISharedPlaytimeManager? _playtimeMan;
//
//    /// <summary>
//    /// Returns true if prototype has no playtime checks or if the client passes them. Return false otherwise.
//    /// For convenience's sake, the failed list will contain all playtime trackers that did not pass the check, hours played and hours required by the check.
//    /// </summary>
//    /// <param name="proto"></param>
//    /// <param name="session"></param>
//    /// <param name="failed"></param>
//    /// <returns></returns>
//    public static bool PlaytimeCheck(this CustomGhostPrototype proto, ICommonSession session, out List<(string tracker, float hoursPlayed, float hoursRequired)> failed)
//    {
//        _playtimeMan ??= IoCManager.Resolve<ISharedPlaytimeManager>();
//
//        failed = new();
//
//        if (proto.PlaytimeHours is null)
//            return true;
//
//        var trackers = proto.PlaytimeHours.Keys;
//
//        if (!_playtimeMan.TryGetTrackerTimes(session, out var playtimes))
//        {
//            foreach (var tracker in trackers)
//                failed.Append((tracker, 0, proto.PlaytimeHours[tracker]));
//            return false;
//        }
//
//        foreach (var tracker in trackers)
//        {
//            float hoursRequired = proto.PlaytimeHours[tracker];
//
//            if (playtimes.TryGetValue(tracker, out var playtime))
//            {
//                failed.Add((tracker, 0, hoursRequired));
//                continue;
//            }
//
//            float hoursPlayed = (float) playtime.TotalHours;
//
//            if (hoursPlayed < hoursRequired)
//            {
//                failed.Add((tracker, 0, hoursRequired));
//                continue;
//            }
//        }
//        return failed.Count == 0;
//    }
//}

public abstract class CustomGhostRestriction
{
    public virtual bool HideOnFail => false;

    public abstract bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason);
}

// omitting CustomGhost prefix for convenience when prototyping
[DataDefinition]
public sealed partial class CkeyRestriction : CustomGhostRestriction
{
    [DataField(required: true)]
    public List<string> Ckey = new();

    public override bool HideOnFail => true;

    public override bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason)
    {
        failReason = null;
        return true;
        var name = player.Name;
        if (player.Name.StartsWith("localhost@"))
            name = name.Substring(9); // what's the proper way of doing this?

        if (Ckey.Contains(name, StringComparer.OrdinalIgnoreCase)) // todo current, invariant or ordinal?
            return true;

        failReason = Loc.GetString("custom-ghost-fail-exclusive-ghost");

        return false;
    }
}

[DataDefinition]
public sealed partial class PlaytimeRestriction : CustomGhostRestriction
{
    private static ISharedPlaytimeManager? _playtime = null;

    [DataField(required: true)]
    public List<string> Jobs = new();

    [DataField(required: true)]
    public float HoursPlaytime;

    [DataField(required: true)]
    public string Title = string.Empty;

    public override bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason)
    {
        _playtime ??= IoCManager.Resolve<ISharedPlaytimeManager>();
        failReason = null;
        if(!_playtime.TryGetTrackerTimes(player, out var playtimes))
        {
            failReason = "Failed to get playtimes. Ask an admin for help if this error persists.";
            return false;
        }

        double total = 0;
        foreach(var job in Jobs)
        {
            if (playtimes.TryGetValue(job, out var time))
                total += time.TotalHours;
        }

        if(total < HoursPlaytime)
        {
            string jobList = $"\"{string.Join("\", \"", Jobs)}\"";
            failReason = Loc.GetString("custom-ghost-fail-insufficient-playtime",
                    ("department", Title),
                    ("jobList", jobList),
                    ("requiredHours", MathF.Round(HoursPlaytime)),
                    ("requiredMinutes", MathF.Round(HoursPlaytime % 1 * 60)),
                    ("playtimeHours", Math.Round(total)),
                    ("playtimeMinutes", Math.Round(total % 1 * 60))
            );
            return false;
        }

        return true;
    }
}
