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
        // 1. Manually draw the background to ensure it's at the very bottom
        handle.DrawRect(new UIBox2(Vector2.Zero, PixelSize), CitiNetBg.WithAlpha(0.95f));

        var offset = GetOffset();

        // 2. Overlay CitiNet-specific Sectors *under* the walls
        if (ShowSectors)
        {
            foreach (var sector in MapSectors)
            {
                // Convert world bounds to local relative positions, then scale to screen
                var leftTop = ScalePositionFlipY(new Vector2(sector.Bounds.Left, sector.Bounds.Top) - offset);
                var rightBottom = ScalePositionFlipY(new Vector2(sector.Bounds.Right, sector.Bounds.Bottom) - offset);
                
                // Note: ScalePositionFlipY handles the Y inversion and MidPoint centering
                handle.DrawRect(new UIBox2(leftTop, rightBottom), sector.Color.WithAlpha(0.3f));
                handle.DrawRect(new UIBox2(leftTop, rightBottom), sector.Color.WithAlpha(0.6f), false); // Border
            }
        }

        // 3. Temporarily hide base background to prevent overwriting sectors
        var originalBg = BackgroundColor;
        BackgroundColor = Color.Transparent;

        // 4. Draw the base NavMap (Tiles, RegionOverlays, Walls)
        base.Draw(handle);

        // Restore base background
        BackgroundColor = originalBg;

        // 5. Draw Dynamic Pings (Layer 4)
        if (MapPings.Count > 0)
        {
            var timeInstance = IoCManager.Resolve<Robust.Shared.Timing.IGameTiming>().RealTime.TotalSeconds;
            
            foreach (var ping in MapPings)
            {
                var pos = ScalePositionFlipY(ping.LocalPosition - offset);
                
                // Calculate pulsing radius based on time
                var cycle = timeInstance % 1.5; // 1.5 second ping cycle
                var progress = (float)(cycle / 1.5); // 0.0 to 1.0 progress
                var currentRadius = ping.Radius * MinimapScale * progress; // Screen scale radius
                
                // Opacity fades out as it expands
                var alpha = 1f - progress;
                
                // Draw expanding ring
                handle.DrawCircle(pos, currentRadius, ping.Color.WithAlpha(alpha), false);
                
                // Draw inner dot
                handle.DrawCircle(pos, 2f, ping.Color);
            }
        }

        // 6. Draw CitiNet-specific Beacons (POIs) with premium typography *over* the walls
        if (ClientBeaconsEnabled)
        {
            var fontSize = (int) Math.Round(1 / WorldRange * DefaultDisplayedRange * UIScale * 10, 0);
            var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), fontSize);
            
            // Icon sizing
            var iconSize = new Vector2(16, 16);
            var halfIconSize = iconSize / 2f;
            
            var entManager = IoCManager.Resolve<IEntityManager>();
            var spriteSys = entManager.System<Robust.Client.GameObjects.SpriteSystem>();

            foreach (var beacon in MapBeacons)
            {
                var pos = ScalePositionFlipY(beacon.LocalPosition - offset);
                var labelPos = pos + new Vector2(10, -5);

                // If we have an icon specified, draw it
                if (beacon.Icon != null)
                {
                    var texture = spriteSys.Frame0(beacon.Icon);
                    if (texture != null)
                    {
                        var rect = new UIBox2(pos - halfIconSize, pos + halfIconSize);
                        handle.DrawTextureRect(texture, rect, beacon.Color);
                        
                        // Draw Label next to the icon
                        handle.DrawString(font, labelPos + new Vector2(1, 1), beacon.Label, Color.Black.WithAlpha(0.5f));
                        handle.DrawString(font, labelPos, beacon.Label, beacon.Color);
                        continue;
                    }
                }
                
                // Fallback: Draw default primitive icon (diamond) if no sprite is set
                handle.DrawCircle(pos, 5f, beacon.Color);
                handle.DrawCircle(pos, 3f, Color.White.WithAlpha(0.8f)); // Inner dot for "premium" look
                
                // Text with shadow for readability
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
