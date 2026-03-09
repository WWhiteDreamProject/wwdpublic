using Content.Shared._NC.CitiNet;
using Robust.Client.GameObjects;

namespace Content.Client._NC.CitiNet;

/// <summary>
/// Система визуализации узла CitiNet.
/// Управляет цветом индикаторов в зависимости от состояния.
/// </summary>
public sealed class CitiNetNodeAppearanceSystem : VisualizerSystem<CitiNetNodeComponent>
{
    // Мы НЕ вызываем SubscribeLocalEvent для AppearanceChangeEvent здесь, 
    // так как VisualizerSystem<T> делает это автоматически.

    protected override void OnAppearanceChange(EntityUid uid, CitiNetNodeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Пытаемся получить состояние из данных внешнего вида
        if (!AppearanceSystem.TryGetData<CitiNetNodeState>(uid, CitiNetNodeVisuals.State, out var state, args.Component))
            return;

        // Определяем цвет индикатора
        var color = state switch
        {
            CitiNetNodeState.Idle => Color.FromHex("#39FF14"),
            CitiNetNodeState.Downloading => Color.FromHex("#FFA500"),
            CitiNetNodeState.Cooldown => Color.FromHex("#FF3131"),
            _ => Color.White
        };

        // Обновляем цвет слоя "server"
        // В YAML мы задали этот слой для отображения состояния
        if (args.Sprite.LayerMapTryGet(CitiNetNodeVisuals.State, out var layer))
        {
            args.Sprite.LayerSetColor(layer, color);
        }
    }
}
