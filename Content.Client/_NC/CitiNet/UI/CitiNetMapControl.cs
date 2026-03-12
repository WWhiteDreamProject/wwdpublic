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

/// <summary>
/// Premium CitiNet Map Control.
/// Inherits from NavMapControl to leverage engine-level performance and features.
/// </summary>
public sealed partial class CitiNetMapControl : NavMapControl
{
    [Dependency] private readonly IResourceCache _cache = default!;

    public List<global::Content.Shared._NC.CitiNet.CitiNetMapSectorData> MapSectors = new();
    public List<global::Content.Shared._NC.CitiNet.CitiNetMapBeaconData> MapBeacons = new();
    public List<global::Content.Shared._NC.CitiNet.CitiNetMapPingData> MapPings = new();

    public bool ShowSectors = true;

    // Premium CitiNet Palette (HSL based)
    private static readonly Color CitiNetBg = Color.FromHex("#05050a"); // Deep space black
    private static readonly Color CitiNetCyan = Color.FromHex("#00f2ff"); // Vibrant neon cyan
    private static readonly Color CitiNetGreen = Color.FromHex("#39ff14"); // Radioactive green

    public CitiNetMapControl()
    {
        IoCManager.InjectDependencies(this);
        
        // Apply Premium Styling to base NavMapControl
        BackgroundColor = CitiNetBg.WithAlpha(0.95f);
        WallColor = CitiNetCyan;
        TileColor = Color.FromHex("#0a1a1a"); // Subtle grid background
    }

    public void Recenter()
    {
        Recentering = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // 1. Manually draw the absolute background
        handle.DrawRect(new UIBox2(Vector2.Zero, PixelSize), CitiNetBg.WithAlpha(0.98f));

        // 2. Clear base background before drawing walls/tiles
        var originalBg = BackgroundColor;
        BackgroundColor = Color.Transparent;

        // 3. Draw base NavMap (Walls, Tiles, etc.)
        base.Draw(handle);
        
        // Restore background color for other systems if needed
        BackgroundColor = originalBg;

        var offset = GetOffset();

        // 4. Draw CitiNet Sectors OVER the base map but with low alpha
        if (ShowSectors)
        {
            var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 12);
            
            foreach (var sector in MapSectors)
            {
                var leftTop = ScalePositionFlipY(new Vector2(sector.Bounds.Left, sector.Bounds.Top) - offset);
                var rightBottom = ScalePositionFlipY(new Vector2(sector.Bounds.Right, sector.Bounds.Bottom) - offset);
                var box = new UIBox2(leftTop, rightBottom);

                // Very faint fill for the district area
                handle.DrawRect(box, sector.Color.WithAlpha(0.15f));
                
                // Slightly stronger border
                handle.DrawRect(box, sector.Color.WithAlpha(0.35f), false);

                // Draw Sector Name in the center
                var center = box.Center;
                var textPos = center - new Vector2(sector.Name.Length * 3.5f, 6); // Rough centering
                
                // Draw name with a shadow for clarity
                handle.DrawString(font, textPos + new Vector2(1, 1), sector.Name, Color.Black.WithAlpha(0.4f));
                handle.DrawString(font, textPos, sector.Name, sector.Color.WithAlpha(0.8f));
            }
        }

        // 5. Draw Dynamic Pings (Layer 4)
        if (MapPings.Count > 0)
        {
            var timeInstance = IoCManager.Resolve<Robust.Shared.Timing.IGameTiming>().RealTime.TotalSeconds;
            
            foreach (var ping in MapPings)
            {
                var pos = ScalePositionFlipY(ping.LocalPosition - offset);
                var cycle = timeInstance % 1.5; 
                var progress = (float)(cycle / 1.5); 
                var currentRadius = ping.Radius * MinimapScale * progress; 
                var alpha = 1f - progress;
                
                handle.DrawCircle(pos, currentRadius, ping.Color.WithAlpha(alpha), false);
                handle.DrawCircle(pos, 2f, ping.Color);
            }
        }

        // 6. Draw POIs (Layer 3)
        if (ClientBeaconsEnabled)
        {
            var fontSize = (int) Math.Round(1 / WorldRange * DefaultDisplayedRange * UIScale * 11, 0);
            var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), fontSize);
            
            var iconSize = new Vector2(16, 16);
            var halfIconSize = iconSize / 2f;
            
            var entManager = IoCManager.Resolve<IEntityManager>();
            var spriteSys = entManager.System<Robust.Client.GameObjects.SpriteSystem>();

            foreach (var beacon in MapBeacons)
            {
                var pos = ScalePositionFlipY(beacon.LocalPosition - offset);
                var labelPos = pos + new Vector2(10, -5);

                if (beacon.Icon != null)
                {
                    var texture = spriteSys.Frame0(beacon.Icon);
                    if (texture != null)
                    {
                        var rect = new UIBox2(pos - halfIconSize, pos + halfIconSize);
                        handle.DrawTextureRect(texture, rect, beacon.Color);
                        handle.DrawString(font, labelPos + new Vector2(1, 1), beacon.Label, Color.Black.WithAlpha(0.5f));
                        handle.DrawString(font, labelPos, beacon.Label, beacon.Color);
                        continue;
                    }
                }
                
                handle.DrawCircle(pos, 5f, beacon.Color);
                handle.DrawString(font, labelPos + new Vector2(1, 1), beacon.Label, Color.Black.WithAlpha(0.5f));
                handle.DrawString(font, labelPos, beacon.Label, beacon.Color);
            }
        }
    }

    /// <summary>
    /// Helper to convert world coordinates to screen pixels within the control.
    /// Uses NavMapControl's scaling math.
    /// </summary>
    private Vector2 WorldToScreen(Vector2 worldPos)
    {
        return ScalePositionFlipY(worldPos - GetOffset());
    }
}
