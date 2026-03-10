// _NC: Кастомная тайл-реакция — спавнит стак предметов вместо лужи.
// Используется для пороха: весь объём реагента превращается в стак MaterialGunpowder на одном тайле.
using Content.Server.Stack;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NC.Chemistry.TileReactions;

[DataDefinition]
public sealed partial class SpillToStackReaction : ITileReaction
{
    /// <summary>
    ///     Прототип энтити, который будет заспавнен (например, MaterialGunpowder).
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity = default!;

    public FixedPoint2 TileReact(TileRef tile,
        ReagentPrototype reagent,
        FixedPoint2 reactVolume,
        IEntityManager entityManager,
        List<ReagentData>? data)
    {
        // Минимум 1 единица для спавна
        if (reactVolume < 1)
            return FixedPoint2.Zero;

        // Получаем серверный StackSystem (не абстрактный SharedStackSystem)
        var stackSystem = entityManager.System<StackSystem>();

        // Координаты центра тайла
        var center = entityManager.System<TurfSystem>().GetTileCenter(tile);

        // Количество предметов = целая часть объёма реагента
        var amount = (int) reactVolume.Float();
        if (amount <= 0)
            return FixedPoint2.Zero;

        // SpawnMultiple сам разобьёт на несколько стаков, если amount > maxStackSize
        stackSystem.SpawnMultiple(Entity, amount, center);

        // Возвращаем сколько единиц реагента было «потреблено» реакцией
        return amount;
    }
}
