// The spiritual successor to CrutchesGigapack.dm
// All code that did not find a proper home for itself will end up here
// It's a good thing we barely have any QA.

// архаичный ритуал уцелел 🥹

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Utility;

namespace Content.Shared._White.Other;

public static partial class Crutches
{
    private static IEntityManager _entMan = default!;
    private static ILogManager _logMan = default!;
    private static ISawmill _log = default!;
    private static void IoCResolve<T>(out T dep) => dep = IoCManager.Resolve<T>();
    
    /// <summary>
    /// This is called in EntryPoint immediately after instantiating IoC graph.
    /// This exists because <see cref="IoCManager.InjectDependencies{T}(T)"/>
    /// does not know what to do with static objects.
    /// </summary>
    public static void InitDependencies()
    {
        IoCResolve(out _entMan);
        IoCResolve(out _logMan);
        _log = _logMan.GetSawmill(nameof(Crutches));
    }

    /// <summary>
    /// This is a direct copypaste of <see cref="EntitySystem.Resolve{TComp}(EntityUid, ref TComp?, bool)"/> for convenience's sake.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Resolve<TComp>(EntityUid uid, [NotNullWhen(true)] ref TComp? component, bool logMissing = true)
        where TComp : IComponent
    {
        DebugTools.AssertOwner(uid, component);

        if (component != null && !component.Deleted)
            return true;

        var found = _entMan.TryGetComponent(uid, out component);

        if (logMissing && !found)
        {
            _log.Error($"Can't resolve \"{typeof(TComp)}\" on entity {_entMan.ToPrettyString(uid)}!\n{Environment.StackTrace}");
        }

        return found;
    }
}