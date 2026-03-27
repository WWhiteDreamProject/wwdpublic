namespace Content.Shared._NC.Cyberware.Events;

/// <summary>
///     Вызывается, когда человечность сущности падает до нуля.
///     Триггер для финальной стадии киберпсихоза.
/// </summary>
public sealed class HumanityZeroEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public HumanityZeroEvent(EntityUid entity)
    {
        Entity = entity;
    }
}