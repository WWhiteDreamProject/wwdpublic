using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.Systems;

/// <summary>
///     Управляет рассудком и запасом человечности.
/// </summary>
public sealed class HumanitySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanityComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, HumanityComponent component, MapInitEvent args)
    {
        var humanity = _random.Next(80, 130);

        component.BaseHumanity = humanity;
        component.MaxHumanity = humanity;
        component.CurrentHumanity = humanity;

        Dirty(uid, component);
    }

    /// <summary>
    ///     Отнимает человечность.
    /// </summary>
    public void DeductHumanity(EntityUid uid, float amount, HumanityComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var oldHumanity = component.CurrentHumanity;
        component.CurrentHumanity = Math.Max(0, component.CurrentHumanity - amount);
        
        if (oldHumanity > 0 && component.CurrentHumanity <= 0)
        {
            RaiseLocalEvent(uid, new HumanityZeroEvent(uid));
        }

        Dirty(uid, component);
    }

    /// <summary>
    ///     Восстанавливает человечность (не может превышать MaxHumanity).
    /// </summary>
    public void RestoreHumanity(EntityUid uid, float amount, HumanityComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.CurrentHumanity = Math.Min(component.MaxHumanity, component.CurrentHumanity + amount);
        Dirty(uid, component);
    }

    /// <summary>
    ///     Снижает максимальный лимит человечности (перманентная травма).
    /// </summary>
    public void ReduceMaxHumanity(EntityUid uid, float amount, HumanityComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.MaxHumanity = Math.Max(0, component.MaxHumanity - amount);
        if (component.CurrentHumanity > component.MaxHumanity)
        {
            component.CurrentHumanity = component.MaxHumanity;
        }
        Dirty(uid, component);
    }
}