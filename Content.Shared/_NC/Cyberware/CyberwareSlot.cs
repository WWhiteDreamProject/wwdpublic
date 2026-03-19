using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware;

/// <summary>
///     Определяет анатомические слоты для установки киберимплантов.
/// </summary>
[Serializable, NetSerializable]
public enum CyberwareSlot : byte
{
    None = 0,
    Neuroport,        // Нейропорт
    Optics,           // Оптика
    Audio,            // Аудио
    RightArm,         // Правая рука
    LeftArm,          // Левая рука
    RightLeg,         // Правая нога
    LeftLeg,          // Левая нога
    Internal,         // Внутренние импланты (Органы)
    External,         // Внешние импланты (Броня, экзоскелеты)
    Style             // Стилевые (Хромкожа, светящиеся тату)
}