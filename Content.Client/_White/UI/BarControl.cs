using Content.Client.Weapons.Ranged.ItemStatus;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.UI;

public sealed class BarControl : Control
{
    private float _fill;
    public float Fill { get => _fill; set { DebugTools.Assert(value >= 0 && value <= 1); _fill = value; } }
    public float Percentage { get => _fill*100f; set { Fill = value / 100; } }
    public int Rows = 1;




    protected override void Draw(DrawingHandleScreen handle)
    {
        // Scale rendering in this control by UIScale.
        var currentTransform = handle.GetTransform();
        handle.SetTransform(Matrix3Helpers.CreateScale(new Vector2(UIScale)) * currentTransform);

        //var countPerRow = CountPerRow(Size.X);

        Color FillColor = Color.Red;
        Color EmptyColor = Color.DarkRed;
        Color AltFillColor = Color.InterpolateBetween(Color.Red, Color.Black, 0.125f);
        Color AltEmptyColor = Color.InterpolateBetween(Color.DarkRed, Color.Black, 0.075f);

        var pos = new Vector2();
        float fillLeft = _fill * Rows;
        float width = Size.X;
        float height = Size.Y;
        float rowHeight = height / Rows;
        // Draw by rows, bottom to top.
        bool alt = false;
        for (var row = Rows-1; row >= 0; row--)
        {
            Color fill = alt ? FillColor : AltFillColor;
            Color empty = alt ? EmptyColor : AltEmptyColor;
            float rowFill = MathF.Min(fillLeft, 1);
            fillLeft -= 1;
            Vector2 topLeft = Position + new Vector2(0, rowHeight * row);
            Vector2 bottomRight = topLeft + new Vector2(Size.X, Size.Y / Rows);
            
            handle.DrawRect(new UIBox2(topLeft, bottomRight), empty);
            if(rowFill > 0)
                handle.DrawRect(new UIBox2(topLeft + new Vector2(Size.X * (1 - rowFill), 0), bottomRight), fill);
            alt = !alt;
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize) => availableSize;
}

