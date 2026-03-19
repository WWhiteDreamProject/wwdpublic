using Content.Shared._NC.Cyberware.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.Systems;

/// <summary>
///     Управляет рассудком и запасом человечности.
/// </summary>
public sealed class HumanitySystem : EntitySystem
{
    /// <summary>
    ///     Отнимает человечность.
    /// </summary>
    public void DeductHumanity(EntityUid uid, float amount, HumanityComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.CurrentHumanity = Math.Max(0, component.CurrentHumanity - amount);
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