using System.Collections.Generic;
using System.Numerics;
using Content.Client.Pinpointer.UI;
using Content.Shared._NC.CitiNet;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._NC.CitiNet.UI;

public sealed partial class CitiNetMapControl : NavMapControl
{
    [Dependency] private readonly IResourceCache _cache = default!;

    public List<global::Content.Shared._NC.CitiNet.CitiNetMapSectorData> MapSectors = new();
    public List<global::Content.Shared._NC.CitiNet.CitiNetMapBeaconData> MapBeacons = new();
    public List<global::Content.Shared._NC.CitiNet.CitiNetMapPingData> MapPings = new();

    public bool ShowSectors = true;

    private static readonly Color CitiNetBg = Color.FromHex("#05050a"); 
    private static readonly Color CitiNetCyan = Color.FromHex("#00f2ff"); 
    private static readonly Color CitiNetRed = Color.FromHex("#ff0033");

    public CitiNetMapControl()
    {
        IoCManager.InjectDependencies(this);
        BackgroundColor = CitiNetBg.WithAlpha(0.95f);
        WallColor = CitiNetCyan;
        TileColor = Color.FromHex("#0a1a1a"); 
    }

    public void Recenter() => Recentering = true;

    protected override void Draw(DrawingHandleScreen handle)
    {
        handle.DrawRect(new UIBox2(Vector2.Zero, PixelSize), CitiNetBg.WithAlpha(0.98f));

        var originalBg = BackgroundColor;
        BackgroundColor = Color.Transparent;
        base.Draw(handle);
        BackgroundColor = originalBg;

        var offset = GetOffset();

        // 1. Draw Sectors
        if (ShowSectors)
        {
            foreach (var sector in MapSectors)
            {
                var leftTop = ScalePositionFlipY(new Vector2(sector.Bounds.Left, sector.Bounds.Top) - offset);
                var rightBottom = ScalePositionFlipY(new Vector2(sector.Bounds.Right, sector.Bounds.Bottom) - offset);
                var box = new UIBox2(leftTop, rightBottom);

                handle.DrawRect(box, sector.Color.WithAlpha(0.15f));
                handle.DrawRect(box, sector.Color.WithAlpha(0.35f), false);

                var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), (int)(sector.FontSize * UIScale));
                var textPos = box.Center - new Vector2(sector.Name.Length * (sector.FontSize * 0.3f), sector.FontSize * 0.5f);
                handle.DrawString(font, textPos, sector.Name, sector.Color.WithAlpha(0.8f));
            }
        }

        // 2. Draw Dynamic Pings
        if (MapPings.Count > 0)
        {
            var timeInstance = IoCManager.Resolve<Robust.Shared.Timing.IGameTiming>().RealTime.TotalSeconds;
            foreach (var ping in MapPings)
            {
                var pos = ScalePositionFlipY(ping.LocalPosition - offset);
                var progress = (float)((timeInstance % 1.5) / 1.5); 
                handle.DrawCircle(pos, ping.Radius * MinimapScale * progress, ping.Color.WithAlpha(1f - progress), false);
                handle.DrawCircle(pos, 2f, ping.Color);
            }
        }

        // 3. Draw Beacons
        if (ClientBeaconsEnabled)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var spriteSys = entManager.System<Robust.Client.GameObjects.SpriteSystem>();

            foreach (var beacon in MapBeacons)
                DrawBeacon(handle, beacon, offset, spriteSys);
        }
    }

    private void DrawBeacon(DrawingHandleScreen handle, CitiNetMapBeaconData beacon, Vector2 offset, Robust.Client.GameObjects.SpriteSystem spriteSys)
    {
        var pos = ScalePositionFlipY(beacon.LocalPosition - offset);
        var labelPos = pos + new Vector2(10, -5);
        var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), (int)(beacon.FontSize * UIScale));

        var color = beacon.IsDead ? CitiNetRed : beacon.Color;
        var label = beacon.IsDead ? $"{beacon.Label} [SGNL LOST]" : beacon.Label;

        // Visual enhancement for 'SELF'
        if (beacon.IsSelf)
        {
            handle.DrawCircle(pos, 8f, color.WithAlpha(0.3f));
            handle.DrawCircle(pos, 4f, color);
        }

        if (beacon.Icon != null)
        {
            var texture = spriteSys.Frame0(beacon.Icon);
            if (texture != null)
            {
                var rect = Box2.CenteredAround(pos, new Vector2(16, 16));
                handle.DrawTextureRect(texture, new UIBox2(rect.Left, rect.Top, rect.Right, rect.Bottom), color.WithAlpha(beacon.IsDead ? 0.6f : 1.0f));
                handle.DrawString(font, labelPos, label, color);
                return;
            }
        }
        
        if (!beacon.IsSelf) handle.DrawCircle(pos, 5f, color);
        handle.DrawString(font, labelPos, label, color);
    }

    private Vector2 WorldToScreen(Vector2 worldPos) => ScalePositionFlipY(worldPos - GetOffset());
}
