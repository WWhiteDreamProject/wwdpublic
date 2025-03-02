using System.Numerics;
using Content.Client.Resources;
using Content.Client.Viewport;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.Contests;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;


namespace Content.Client.Weapons.Ranged;

// WWDP EDIT START
// so much shit was tossed around and changed, it's easier to assume
// the entire file is changed.
[Virtual]
public class GunSpreadOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected IEntityManager _entManager;
    protected readonly IEyeManager _eye;
    protected readonly IGameTiming _timing;
    protected readonly IInputManager _input;
    protected readonly IPlayerManager _player;
    protected readonly GunSystem _guns;
    protected readonly SharedTransformSystem _transform;
    protected readonly ContestsSystem _contest;

    public GunSpreadOverlay(IEntityManager entManager, IEyeManager eyeManager, IGameTiming timing, IInputManager input, IPlayerManager player, GunSystem system, SharedTransformSystem transform, ContestsSystem contest)
    {
        _entManager = entManager;
        _eye = eyeManager;
        _input = input;
        _timing = timing;
        _player = player;
        _guns = system;
        _transform = transform;
        _contest = contest;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;

        var player = _player.LocalEntity;

        if (player == null ||
            !_entManager.TryGetComponent<TransformComponent>(player, out var xform))
        {
            Reset();
            return;
        }

        var mapPos = _transform.GetMapCoordinates(player.Value, xform: xform);

        if (mapPos.MapId == MapId.Nullspace)
        {
            Reset();
            return;
        }
        if (!_guns.TryGetGun(player.Value, out var gunUid, out var gun))
        {
            Reset();
            return;
        }

        var contest = 1 / _contest.MassContest(player);

        var mouseScreenPos = _input.MouseScreenPosition;
        var mousePos = _eye.PixelToMap(mouseScreenPos);

        if (mapPos.MapId != mousePos.MapId)
        {
            Reset();
            return;
        }

        // (☞ﾟヮﾟ)☞
        double timeSinceLastFire = (_timing.CurTime - gun.CurrentAngleLastUpdate).TotalSeconds;
        double timeSinceLastBonusUpdate = (_timing.CurTime - gun.BonusAngleLastUpdate).TotalSeconds;
        double maxBonusSpread = gun.MaxBonusAngleModified * contest / 2;
        double minSpread = Math.Max(gun.MinAngleModified, 0) * contest / 2;
        double maxSpread = gun.MaxAngleModified * contest / 2;
        
        double bonusSpread = new Angle(MathHelper.Clamp((gun.BonusAngle - gun.BonusAngleDecayModified * timeSinceLastBonusUpdate) / 2,
                                                        0, maxBonusSpread)) * contest;
        double currentAngle = new Angle(MathHelper.Clamp((gun.CurrentAngle.Theta - gun.AngleDecayModified.Theta * timeSinceLastFire) / 2,
                                                        minSpread, maxSpread)) * contest;
        var direction = (mousePos.Position - mapPos.Position);

        Vector2 from = mapPos.Position;
        Vector2 to = mousePos.Position + direction;

        DrawSpread(worldHandle, gun, from, direction, timeSinceLastFire, maxBonusSpread, bonusSpread, maxSpread, minSpread, currentAngle);
    }

    protected virtual void DrawCone(DrawingHandleWorld handle, Vector2 from, Vector2 direction, Angle angle, Color color, float lerp = 1f)
    {
        var dir1 = angle.RotateVec(direction);
        var dir2 = (-angle).RotateVec(direction);
        handle.DrawLine(from + dir1 * (1 - lerp), from + dir1 * (1 + lerp), color);
        handle.DrawLine(from + dir2 * (1 - lerp), from + dir2 * (1 + lerp), color);
    }

    protected virtual void DrawSpread(DrawingHandleWorld worldHandle, GunComponent gun, Vector2 from, Vector2 direction, double timeSinceLastFire, Angle maxBonusSpread, Angle bonusSpread, Angle maxSpread, Angle minSpread, Angle currentAngle)
    {
        worldHandle.DrawLine(from, direction*2, Color.Orange);

        // Show max spread either side
        DrawCone(worldHandle, from, direction, maxSpread + bonusSpread, Color.Red);

        // Show min spread either side
        DrawCone(worldHandle, from, direction, minSpread + bonusSpread, Color.Green);

        // Show current angle
        DrawCone(worldHandle, from, direction, currentAngle + bonusSpread, Color.Yellow);

        DrawCone(worldHandle, from, direction, maxBonusSpread, Color.BetterViolet);

        DrawCone(worldHandle, from, direction, bonusSpread, Color.Violet);

        var oldTheta = MathHelper.Clamp(gun.CurrentAngle - gun.AngleDecayModified * timeSinceLastFire, gun.MinAngleModified, gun.MaxAngleModified);
        var newTheta = MathHelper.Clamp(oldTheta + gun.AngleIncreaseModified.Theta, gun.MinAngleModified.Theta, gun.MaxAngleModified.Theta);
        var shit = new Angle(newTheta + bonusSpread);
        DrawCone(worldHandle, from, direction, shit, Color.Gray);
    }

    protected virtual void Reset() { }

}


public sealed class PartialGunSpreadOverlay : GunSpreadOverlay
{
    private SpriteSystem _sprite;
    private GunComponent? _lastGun;

    private double SmoothedCurrentAngle;
    private double SmoothedBonusSpread;

    private Texture _textureS;
    private Texture _textureL;

    public PartialGunSpreadOverlay(IEntityManager entManager, IEyeManager eyeManager, IGameTiming timing, IInputManager input, IPlayerManager player, GunSystem system, SharedTransformSystem transform, ContestsSystem contest, SpriteSystem sprite) : base(entManager, eyeManager, timing, input, player, system, transform, contest)
    {
        _sprite = sprite;
        _textureS = _sprite.Frame0(new SpriteSpecifier.Texture(new ResPath("/Textures/_White/Interface/gun-spread-marker-s.png")));
        _textureL = _sprite.Frame0(new SpriteSpecifier.Texture(new ResPath("/Textures/_White/Interface/gun-spread-marker-l.png")));
    }

    protected override void Reset() { _lastGun = null; }

    protected override void DrawSpread(DrawingHandleWorld worldHandle, GunComponent gun, Vector2 from, Vector2 direction, double timeSinceLastFire, Angle maxBonusSpread, Angle bonusSpread, Angle maxSpread, Angle minSpread, Angle currentAngle)
    {
        if (_lastGun != gun)
        {
            _lastGun = gun;
            SmoothedCurrentAngle = currentAngle;
            SmoothedBonusSpread = bonusSpread;
        }
        else
        {
            SmoothedCurrentAngle = Double.Lerp(SmoothedCurrentAngle, currentAngle, 0.7);
            SmoothedBonusSpread = Double.Lerp(SmoothedBonusSpread, bonusSpread, 0.35);
        }
        const float third = 1f / 3f;
        float L = (float) ((currentAngle - minSpread) / (maxSpread - minSpread)); // not smoothed
        float hue = Math.Clamp(third - third * L, 0, third);
        Color color = Color.FromHsv(new Robust.Shared.Maths.Vector4(hue, 1, 1, 1));
        DrawCone(worldHandle, from, direction, SmoothedCurrentAngle + SmoothedBonusSpread, color, 0.15f);
        DrawCone(worldHandle, from, direction, SmoothedCurrentAngle, color.WithAlpha(0.33f), 0.15f);
    }

    protected override void DrawCone(DrawingHandleWorld handle, Vector2 from, Vector2 direction, Angle angle, Color color, float lineLerp = 1f)
    {
        var mpp = 1f / EyeManager.PixelsPerMeter;

        var dir1 = angle.RotateVec(direction);
        var dir2 = (-angle).RotateVec(direction);

        Angle negRot = -_eye.CurrentEye.Rotation;

        handle.SetTransform(from, 0);

        Angle ang1 = dir1.ToAngle();
        Angle ang2 = dir2.ToAngle();
        handle.DrawTextureCentered(_textureL, dir1 * 0.76f, ang1, color);
        handle.DrawTextureCentered(_textureL, dir2 * 0.76f, ang2, color);
        handle.DrawTextureCentered(_textureS, dir1 * 0.88f, ang1, color);
        handle.DrawTextureCentered(_textureS, dir2 * 0.88f, ang2, color);
        handle.DrawTextureCentered(_textureL, dir1 * 1f,    ang1, color);
        handle.DrawTextureCentered(_textureL, dir2 * 1f,    ang2, color);
        handle.DrawTextureCentered(_textureS, dir1 * 1.12f, ang1, color);
        handle.DrawTextureCentered(_textureS, dir2 * 1.12f, ang2, color);
        handle.DrawTextureCentered(_textureL, dir1 * 1.24f, ang1, color);
        handle.DrawTextureCentered(_textureL, dir2 * 1.24f, ang2, color);

    }
}   

public static class DrawingHandleWorldExt
{
    public static void DrawTextureCentered(this DrawingHandleWorld handle, Texture tex, Vector2 position, Color? color = null) =>
        handle.DrawTextureRect(tex, Box2.CenteredAround(position, tex.Size / (float) EyeManager.PixelsPerMeter), color);

    public static void DrawTextureCentered(this DrawingHandleWorld handle, Texture tex, Vector2 position, Angle angle, Color? color = null) =>
        handle.DrawTextureRect(tex, new Box2Rotated(Box2.CenteredAround(position, tex.Size / (float) EyeManager.PixelsPerMeter), angle, position), color);

    public static void DrawRectCentered(this DrawingHandleWorld handle, Vector2 position, Vector2 size, Color color, bool filled = false) =>
        handle.DrawRect(Box2.CenteredAround(position, size), color, filled);

    public static void DrawRectCentered(this DrawingHandleWorld handle, Vector2 position, Vector2 size, Angle rot, Color color, bool filled = false) =>
        handle.DrawRect(new Box2Rotated(Box2.CenteredAround(position, size), rot, position), color, filled);
}
