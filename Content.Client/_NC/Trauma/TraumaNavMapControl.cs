using Content.Client.Pinpointer.UI;

namespace Content.Client._NC.Trauma;

/// <summary>
/// Кастомный NavMapControl для планшета Trauma Team.
/// Отключает перетаскивание карты, оставляя зум колёсиком мыши.
/// </summary>
public sealed class TraumaNavMapControl : NavMapControl
{
    // Запрещаем drag — перетаскивать карту нельзя
    protected override bool Draggable => false;

    /// <summary>
    /// Отключает авто-центрирование карты (Recentering — protected).
    /// </summary>
    public void DisableRecentering()
    {
        Recentering = false;
    }
}
