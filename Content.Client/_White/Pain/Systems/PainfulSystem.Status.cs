using Content.Client.Alerts;
using Content.Shared._White.Pain.Components;

namespace Content.Client._White.Pain.Systems;

public sealed partial class PainfulSystem
{
    private void InitializeStatus()
    {
        SubscribeLocalEvent<PainStatusComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    #region Event Handling

    private void OnUpdateAlertSprite(Entity<PainStatusComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.Alert)
            return;

        var sprite = args.SpriteViewEnt.AsNullable();

        foreach (var (location, level) in ent.Comp.PainStatus)
        {
            if (!ent.Comp.Layers.TryGetValue(location, out var layer))
                continue;

            var state = $"{location}_{level}".ToLower();
            _system.LayerSetRsiState(sprite, layer, state);
        }
    }

    #endregion
}
