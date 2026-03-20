using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware;

/// <summary>
///     Категории анатомических зон для установки киберимплантов.
///     Используется в CyberwareImplantComponent для указания совместимости.
/// </summary>
[Serializable, NetSerializable]
public enum CyberwareCategory : byte
{
    None = 0,
    Neuroport,    // Нейропорт (3 слота)
    Optics,       // Кибероптика (6 слотов)
    Audio,        // Аудиоимпланты (2 слота)
    RightArm,     // Правая рука (4 слота)
    LeftArm,      // Левая рука (4 слота)
    RightLeg,     // Правая нога (4 слота)
    LeftLeg,      // Левая нога (4 слота)
    Internal,     // Внутренние импланты (5 слотов)
    External,     // Внешние импланты (4 слота)
    Style         // Стилевые импланты (5 слотов)
}

/// <summary>
///     Конкретные пронумерованные слоты для киберимплантов.
///     Каждая категория содержит несколько слотов (S1, S2...).
///     Используется как ключ в словаре InstalledImplants.
/// </summary>
[Serializable, NetSerializable]
public enum CyberwareSlot : byte
{
    None = 0,

    // Нейропорт (3 слота)
    Neuroport1 = 1, Neuroport2, Neuroport3,

    // Кибероптика (6 слотов: правый глаз 3 + левый глаз 3)
    Optics1 = 10, Optics2, Optics3, Optics4, Optics5, Optics6,

    // Аудио (2 слота)
    Audio1 = 20, Audio2,

    // Правая рука (4 слота)
    RightArm1 = 30, RightArm2, RightArm3, RightArm4,

    // Левая рука (4 слота)
    LeftArm1 = 40, LeftArm2, LeftArm3, LeftArm4,

    // Правая нога (4 слота)
    RightLeg1 = 50, RightLeg2, RightLeg3, RightLeg4,

    // Левая нога (4 слота)
    LeftLeg1 = 60, LeftLeg2, LeftLeg3, LeftLeg4,

    // Внутренние (5 слотов)
    Internal1 = 70, Internal2, Internal3, Internal4, Internal5,

    // Внешние (4 слота)
    External1 = 80, External2, External3, External4,

    // Стилевые (5 слотов)
    Style1 = 90, Style2, Style3, Style4, Style5,
}

/// <summary>
///     Утилитный класс для работы со слотами и категориями кибервари.
/// </summary>
public static class CyberwareSlotHelper
{
    /// <summary>Маппинг категория → массив слотов.</summary>
    private static readonly Dictionary<CyberwareCategory, CyberwareSlot[]> CategorySlots = new()
    {
        { CyberwareCategory.Neuroport, new[] { CyberwareSlot.Neuroport1, CyberwareSlot.Neuroport2, CyberwareSlot.Neuroport3 } },
        { CyberwareCategory.Optics, new[] { CyberwareSlot.Optics1, CyberwareSlot.Optics2, CyberwareSlot.Optics3, CyberwareSlot.Optics4, CyberwareSlot.Optics5, CyberwareSlot.Optics6 } },
        { CyberwareCategory.Audio, new[] { CyberwareSlot.Audio1, CyberwareSlot.Audio2 } },
        { CyberwareCategory.RightArm, new[] { CyberwareSlot.RightArm1, CyberwareSlot.RightArm2, CyberwareSlot.RightArm3, CyberwareSlot.RightArm4 } },
        { CyberwareCategory.LeftArm, new[] { CyberwareSlot.LeftArm1, CyberwareSlot.LeftArm2, CyberwareSlot.LeftArm3, CyberwareSlot.LeftArm4 } },
        { CyberwareCategory.RightLeg, new[] { CyberwareSlot.RightLeg1, CyberwareSlot.RightLeg2, CyberwareSlot.RightLeg3, CyberwareSlot.RightLeg4 } },
        { CyberwareCategory.LeftLeg, new[] { CyberwareSlot.LeftLeg1, CyberwareSlot.LeftLeg2, CyberwareSlot.LeftLeg3, CyberwareSlot.LeftLeg4 } },
        { CyberwareCategory.Internal, new[] { CyberwareSlot.Internal1, CyberwareSlot.Internal2, CyberwareSlot.Internal3, CyberwareSlot.Internal4, CyberwareSlot.Internal5 } },
        { CyberwareCategory.External, new[] { CyberwareSlot.External1, CyberwareSlot.External2, CyberwareSlot.External3, CyberwareSlot.External4 } },
        { CyberwareCategory.Style, new[] { CyberwareSlot.Style1, CyberwareSlot.Style2, CyberwareSlot.Style3, CyberwareSlot.Style4, CyberwareSlot.Style5 } },
    };

    /// <summary>Базовые byte-значения enum для вычисления индекса слота.</summary>
    private static readonly Dictionary<CyberwareCategory, byte> CategoryBase = new()
    {
        { CyberwareCategory.Neuroport, 1 },
        { CyberwareCategory.Optics, 10 },
        { CyberwareCategory.Audio, 20 },
        { CyberwareCategory.RightArm, 30 },
        { CyberwareCategory.LeftArm, 40 },
        { CyberwareCategory.RightLeg, 50 },
        { CyberwareCategory.LeftLeg, 60 },
        { CyberwareCategory.Internal, 70 },
        { CyberwareCategory.External, 80 },
        { CyberwareCategory.Style, 90 },
    };

    /// <summary>Возвращает все слоты указанной категории.</summary>
    public static CyberwareSlot[] GetSlots(CyberwareCategory category)
    {
        return CategorySlots.TryGetValue(category, out var slots) ? slots : Array.Empty<CyberwareSlot>();
    }

    /// <summary>Определяет категорию по конкретному слоту.</summary>
    public static CyberwareCategory GetCategory(CyberwareSlot slot)
    {
        var val = (byte)slot;
        if (val >= 1 && val <= 3) return CyberwareCategory.Neuroport;
        if (val >= 10 && val <= 15) return CyberwareCategory.Optics;
        if (val >= 20 && val <= 21) return CyberwareCategory.Audio;
        if (val >= 30 && val <= 33) return CyberwareCategory.RightArm;
        if (val >= 40 && val <= 43) return CyberwareCategory.LeftArm;
        if (val >= 50 && val <= 53) return CyberwareCategory.RightLeg;
        if (val >= 60 && val <= 63) return CyberwareCategory.LeftLeg;
        if (val >= 70 && val <= 74) return CyberwareCategory.Internal;
        if (val >= 80 && val <= 83) return CyberwareCategory.External;
        if (val >= 90 && val <= 94) return CyberwareCategory.Style;
        return CyberwareCategory.None;
    }

    /// <summary>Возвращает 1-based индекс слота внутри его категории.</summary>
    public static int GetSlotIndex(CyberwareSlot slot)
    {
        var category = GetCategory(slot);
        if (category == CyberwareCategory.None) return 0;
        return (byte)slot - CategoryBase[category] + 1;
    }

    /// <summary>Находит первый свободный слот в категории.</summary>
    public static CyberwareSlot? FindFreeSlot(CyberwareCategory category, ICollection<CyberwareSlot> occupiedSlots)
    {
        foreach (var slot in GetSlots(category))
        {
            if (!occupiedSlots.Contains(slot))
                return slot;
        }
        return null;
    }

    /// <summary>Возвращает отображаемое имя категории для UI.</summary>
    public static string GetCategoryDisplayName(CyberwareCategory category)
    {
        return category switch
        {
            CyberwareCategory.Neuroport => "НЕЙРОПОРТ",
            CyberwareCategory.Optics => "КИБЕРОПТИКА",
            CyberwareCategory.Audio => "АУДИОИМПЛАНТ",
            CyberwareCategory.RightArm => "ПРАВАЯ РУКА",
            CyberwareCategory.LeftArm => "ЛЕВАЯ РУКА",
            CyberwareCategory.RightLeg => "ПРАВАЯ НОГА",
            CyberwareCategory.LeftLeg => "ЛЕВАЯ НОГА",
            CyberwareCategory.Internal => "ВНУТРЕННИЕ",
            CyberwareCategory.External => "ВНЕШНИЕ",
            CyberwareCategory.Style => "СТИЛЕВЫЕ",
            _ => "???",
        };
    }
}