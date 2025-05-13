using Content.Client.Weapons.Ranged.ItemStatus;
using Robust.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.UI;

public sealed class BarBulletRenderer : BaseBulletRenderer
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        // Scale rendering in this control by UIScale.
        var currentTransform = handle.GetTransform();
        handle.SetTransform(Matrix3Helpers.CreateScale(new Vector2(UIScale)) * currentTransform);

        //var countPerRow = CountPerRow(Size.X);

        var pos = new Vector2();

        var spent = Capacity - Count;
        float capacityPerRow = Capacity / Rows;
        float bullets = Count;
        float width = Size.X;
        float height = Size.Y;
        float rowHeight = height / Rows;
        // Draw by rows, bottom to top.
        for (var row = 0; row < Rows; row++)
        {
            float percentage = MathF.Min(bullets / capacityPerRow, 1);
            bullets -= capacityPerRow;

            Vector2 topLeft = Position + new Vector2(0, rowHeight * (Rows - row - 1));
            Vector2 bottomRight = topLeft + new Vector2(Size.X, Size.Y / Rows);
            
            handle.DrawRect(new UIBox2(topLeft, bottomRight), Color.DarkRed);
            if (percentage <= 0)
                continue;
            handle.DrawRect(new UIBox2(topLeft + new Vector2(Size.X * (1 - percentage), 0), bottomRight), Color.Red);
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize) => availableSize;

    protected override void DrawItem(DrawingHandleScreen handle, Vector2 renderPos, bool spent, bool altColor) => throw new NotImplementedException();
}

