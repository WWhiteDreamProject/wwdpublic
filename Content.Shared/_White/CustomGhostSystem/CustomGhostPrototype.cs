using Content.Shared.Ghost;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
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
