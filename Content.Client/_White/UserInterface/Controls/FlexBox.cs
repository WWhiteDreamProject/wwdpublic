using System.Linq;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;


namespace Content.Client._White.UserInterface.Controls;


/// <summary>
///     A container that implements CSS-like Flexbox layout for its children.
/// </summary>
[Virtual]
public class FlexBox : Container
{
    public enum FlexAlignContent
    {
        FlexStart,
        FlexEnd,
        Center,
        Stretch,
        SpaceBetween,
        SpaceAround
    }

    public enum FlexAlignItems
    {
        FlexStart,
        FlexEnd,
        Center,
        Stretch
    }

    public enum FlexDirection
    {
        Row,
        RowReverse,
        Column,
        ColumnReverse
    }

    public enum FlexJustifyContent
    {
        FlexStart,
        FlexEnd,
        Center,
        SpaceBetween,
        SpaceAround,
        SpaceEvenly
    }

    public enum FlexWrap
    {
        NoWrap,
        Wrap,
        WrapReverse
    }

    public const string StylePropertyGap = "gap";
    public const string StylePropertyRowGap = "row-gap";
    public const string StylePropertyColumnGap = "column-gap";
    public const string StylePropertyAlignItems = "align-items";
    public const string StylePropertyOrder = "order";

    private const float DefaultGap = 0f;
    private FlexAlignContent _alignContent = FlexAlignContent.FlexStart;
    private FlexAlignItems _alignItems = FlexAlignItems.Stretch;

    private FlexDirection _direction = FlexDirection.Row;
    private FlexJustifyContent _justifyContent = FlexJustifyContent.FlexStart;
    private FlexWrap _wrap = FlexWrap.Wrap;

    /// <summary>
    ///     Main axis direction for flex items.
    /// </summary>
    public FlexDirection Direction
    {
        get => _direction;
        set
        {
            _direction = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    ///     Whether flex items should wrap onto multiple lines.
    /// </summary>
    public FlexWrap Wrap
    {
        get => _wrap;
        set
        {
            _wrap = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    ///     Alignment of items along the cross axis.
    /// </summary>
    public FlexAlignItems AlignItems
    {
        get => _alignItems;
        set
        {
            _alignItems = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    ///     Alignment of items along the main axis.
    /// </summary>
    public FlexJustifyContent JustifyContent
    {
        get => _justifyContent;
        set
        {
            _justifyContent = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    ///     Alignment of lines along the cross axis when there's extra space.
    /// </summary>
    public FlexAlignContent AlignContent
    {
        get => _alignContent;
        set
        {
            _alignContent = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    ///     Custom gap between items (overrides row-gap and column-gap if set).
    /// </summary>
    public float? GapOverride { get; set; }

    /// <summary>
    ///     Custom row gap between items.
    /// </summary>
    public float? RowGapOverride { get; set; }

    /// <summary>
    ///     Custom column gap between items.
    /// </summary>
    public float? ColumnGapOverride { get; set; }

    private float ActualGap => GetStyleFloat(StylePropertyGap, GapOverride, DefaultGap);
    private float ActualRowGap => GetStyleFloat(StylePropertyRowGap, RowGapOverride, ActualGap);
    private float ActualColumnGap => GetStyleFloat(StylePropertyColumnGap, ColumnGapOverride, ActualGap);

    private float GetStyleFloat(string property, float? overrideValue, float defaultValue)
    {
        if (overrideValue.HasValue)
            return overrideValue.Value;

        if (TryGetStyleProperty(property, out float value))
            return value;

        return defaultValue;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var visibleChildren = Children.Where(c => c.Visible).ToList();
        if (visibleChildren.Count == 0)
            return Vector2.Zero;

        var isRowDirection = Direction == FlexDirection.Row || Direction == FlexDirection.RowReverse;
        var isWrap = Wrap != FlexWrap.NoWrap;

        // Get gaps
        var rowGap = ActualRowGap;
        var columnGap = ActualColumnGap;

        // First pass: measure all children with infinite space to get their base sizes
        foreach (var child in visibleChildren)
            child.Measure(Vector2.PositiveInfinity);

        // Calculate line breaks if wrapping
        var lines = new List<FlexLine>();
        var currentLine = new FlexLine();
        var mainAxisSize = isRowDirection ? availableSize.X : availableSize.Y;
        var crossAxisSize = isRowDirection ? availableSize.Y : availableSize.X;

        // Sort children by order property if needed
        var orderedChildren = visibleChildren.OrderBy(c => GetFlexOrder(c)).ToList();

        foreach (var child in orderedChildren)
        {
            var childSize = GetFlexBaseSize(child, isRowDirection);
            var mainSize = isRowDirection ? child.DesiredSize.X : child.DesiredSize.Y;
            var crossSize = isRowDirection ? child.DesiredSize.Y : child.DesiredSize.X;

            if (isWrap && currentLine.MainSize + childSize > mainAxisSize && currentLine.Items.Count > 0)
            {
                lines.Add(currentLine);
                currentLine = new();
            }

            currentLine.AddItem(child, childSize, mainSize, crossSize);
        }

        if (currentLine.Items.Count > 0)
            lines.Add(currentLine);

        // Calculate total size based on lines
        var totalMainSize = 0f;
        var totalCrossSize = 0f;

        foreach (var line in lines)
        {
            totalMainSize = Math.Max(totalMainSize, line.MainSize);
            totalCrossSize += line.CrossSize;
        }

        // Add gaps between lines
        if (lines.Count > 1)
            totalCrossSize += rowGap * (lines.Count - 1);

        // Add gaps between items in lines
        foreach (var line in lines)
            if (line.Items.Count > 1)
                totalMainSize += columnGap * (line.Items.Count - 1);

        return isRowDirection
            ? new Vector2(totalCrossSize, totalCrossSize)
            : new Vector2(totalCrossSize, totalCrossSize);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var visibleChildren = Children.Where(c => c.Visible).ToList();
        if (visibleChildren.Count == 0)
            return finalSize;

        var isRowDirection = Direction == FlexDirection.Row || Direction == FlexDirection.RowReverse;
        var isRowReverse = Direction == FlexDirection.RowReverse;
        var isColumnReverse = Direction == FlexDirection.ColumnReverse;
        var isWrap = Wrap != FlexWrap.NoWrap;
        var isWrapReverse = Wrap == FlexWrap.WrapReverse;

        // Get gaps
        var rowGap = ActualRowGap;
        var columnGap = ActualColumnGap;

        // First pass: measure children
        foreach (var child in visibleChildren)
            child.Measure(Vector2.PositiveInfinity);

        // Calculate layout
        var lines = CalculateFlexLines(visibleChildren, finalSize, isRowDirection, isWrap, rowGap, columnGap);

        // Position lines based on alignment
        PositionFlexLines(lines, finalSize, isRowDirection, isWrapReverse);

        // Position items within each line
        foreach (var line in lines)
            PositionLineItems(line, isRowDirection, isRowReverse, isColumnReverse, columnGap, rowGap, finalSize);

        return finalSize;
    }

    private List<FlexLine> CalculateFlexLines(
        List<Control> children,
        Vector2 availableSize,
        bool isRowDirection,
        bool isWrap,
        float rowGap,
        float columnGap
    )
    {
        var lines = new List<FlexLine>();
        var currentLine = new FlexLine();
        var mainAxisSize = isRowDirection ? availableSize.X : availableSize.Y;
        var crossAxisSize = isRowDirection ? availableSize.Y : availableSize.X;

        var gap = isRowDirection ? rowGap : columnGap;

        var orderedChildren = children.OrderBy(c => GetFlexOrder(c)).ToList();

        foreach (var child in orderedChildren)
        {
            var childSize = GetFlexBaseSize(child, isRowDirection);
            var mainSize = isRowDirection ? child.DesiredSize.X : child.DesiredSize.Y;
            var crossSize = isRowDirection ? child.DesiredSize.Y : child.DesiredSize.X;

            // Check if we need to wrap
            if (isWrap && currentLine.Items.Count > 0)
            {
                var requiredSpace = currentLine.MainSize + gap + childSize;
                if (requiredSpace > mainAxisSize)
                {
                    lines.Add(currentLine);
                    currentLine = new();
                }
            }

            currentLine.AddItem(child, childSize, mainSize, crossSize);
        }

        if (currentLine.Items.Count > 0)
            lines.Add(currentLine);

        return lines;
    }

    private void PositionFlexLines(
        List<FlexLine> lines,
        Vector2 containerSize,
        bool isRowDirection,
        bool isWrapReverse
    )
    {
        var totalCrossSize = lines.Sum(l => l.CrossSize);
        var containerCrossSize = isRowDirection ? containerSize.Y : containerSize.X;
        var gap = isRowDirection ? ActualRowGap : ActualColumnGap;

        if (lines.Count > 1)
            totalCrossSize += gap * (lines.Count - 1);

        var crossStartPosition = 0f;
        switch (AlignContent)
        {
            case FlexAlignContent.FlexStart:
                crossStartPosition = 0;
                break;
            case FlexAlignContent.Center:
                crossStartPosition = Math.Max(0, (containerCrossSize - totalCrossSize) / 2);
                break;
            case FlexAlignContent.FlexEnd:
                crossStartPosition = Math.Max(0, containerCrossSize - totalCrossSize);
                break;
            case FlexAlignContent.SpaceBetween:
                // Gaps will be distributed in the positioning loop
                break;
            case FlexAlignContent.SpaceAround:
                // Equal space around each line
                var spacePerLine = Math.Max(0, (containerCrossSize - totalCrossSize) / lines.Count);
                crossStartPosition = spacePerLine / 2;
                break;
        }

        var currentCrossPos = crossStartPosition;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineIndex = isWrapReverse ? lines.Count - 1 - i : i;

            if (AlignContent == FlexAlignContent.SpaceBetween && lines.Count > 1 && i < lines.Count - 1)
            {
                var remainingSpace = containerCrossSize - currentCrossPos - totalCrossSize;
                var gapSize = remainingSpace / (lines.Count - i - 1);
                line.CrossPosition = currentCrossPos;
                currentCrossPos += line.CrossSize + gapSize;
            }
            else
            {
                line.CrossPosition = currentCrossPos;
                currentCrossPos += line.CrossSize + (i < lines.Count - 1 ? gap : 0);
            }

            line.LineIndex = lineIndex;
        }
    }

    private void PositionLineItems(
        FlexLine line,
        bool isRowDirection,
        bool isRowReverse,
        bool isColumnReverse,
        float columnGap,
        float rowGap,
        Vector2 containerSize
    )
    {
        var mainAxisSize = isRowDirection ? containerSize.X : containerSize.Y;
        var crossAxisSize = isRowDirection ? containerSize.Y : containerSize.X;
        var lineMainSize = line.MainSize;
        var itemGap = isRowDirection ? columnGap : rowGap;

        if (line.Items.Count > 1)
            lineMainSize += itemGap * (line.Items.Count - 1);

        var mainStartPosition = 0f;
        switch (JustifyContent)
        {
            case FlexJustifyContent.FlexStart:
                mainStartPosition = 0;
                break;
            case FlexJustifyContent.Center:
                mainStartPosition = Math.Max(0, (mainAxisSize - lineMainSize) / 2);
                break;
            case FlexJustifyContent.FlexEnd:
                mainStartPosition = Math.Max(0, mainAxisSize - lineMainSize);
                break;
            case FlexJustifyContent.SpaceBetween:
                mainStartPosition = 0;
                break;
            case FlexJustifyContent.SpaceAround:
                var spacePerItem = Math.Max(0, (mainAxisSize - lineMainSize) / line.Items.Count);
                mainStartPosition = spacePerItem / 2;
                break;
            case FlexJustifyContent.SpaceEvenly:
                var spaceBetween = Math.Max(0, (mainAxisSize - lineMainSize) / (line.Items.Count + 1));
                mainStartPosition = spaceBetween;
                break;
        }

        var currentMainPos = mainStartPosition;
        var items = isRowReverse || isColumnReverse ? line.Items.AsEnumerable().Reverse().ToList() : line.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var itemSize = item.MainSize;
            var crossSize = line.CrossSize;

            // Calculate cross position based on align-items
            var crossPosition = CalculateCrossPosition(item.Control, line.CrossPosition, crossSize, isRowDirection);

            // Calculate main position
            var mainPosition = currentMainPos;
            if (JustifyContent == FlexJustifyContent.SpaceBetween && items.Count > 1 && i < items.Count - 1)
            {
                var remainingSpace = mainAxisSize - currentMainPos - lineMainSize;
                var gapSize = remainingSpace / (items.Count - i - 1);
                currentMainPos += itemSize + gapSize;
            }
            else
                currentMainPos += itemSize + (i < items.Count - 1 ? itemGap : 0);

            // Arrange the item
            ArrangeItem(item.Control, mainPosition, crossPosition, itemSize, crossSize, isRowDirection);
        }
    }

    private float CalculateCrossPosition(Control child, float lineCrossPos, float lineCrossSize, bool isRowDirection)
    {
        var childCrossSize = isRowDirection ? child.DesiredSize.Y : child.DesiredSize.X;
        var align = GetEffectiveAlignItems(child);

        switch (align)
        {
            case FlexAlignItems.FlexStart:
                return lineCrossPos;
            case FlexAlignItems.Center:
                return lineCrossPos + Math.Max(0, (lineCrossSize - childCrossSize) / 2);
            case FlexAlignItems.FlexEnd:
                return lineCrossPos + Math.Max(0, lineCrossSize - childCrossSize);
            case FlexAlignItems.Stretch:
            default:
                return lineCrossPos;
        }
    }

    private void ArrangeItem(
        Control child,
        float mainPos,
        float crossPos,
        float mainSize,
        float crossSize,
        bool isRowDirection
    )
    {
        UIBox2 rect;
        if (isRowDirection)
        {
            rect = new(
                mainPos,
                crossPos,
                mainPos + mainSize,
                crossPos + crossSize
            );
        }
        else
        {
            rect = new(
                crossPos,
                mainPos,
                crossPos + crossSize,
                mainPos + mainSize
            );
        }

        child.Arrange(rect);
    }

    private float GetFlexBaseSize(Control child, bool isRowDirection) =>
        // In a real implementation, this would consider flex-basis, flex-grow, and flex-shrink
        isRowDirection ? child.DesiredSize.X : child.DesiredSize.Y;

    private int GetFlexOrder(Control child)
    {
        if (child.TryGetStyleProperty(StylePropertyOrder, out int order))
            return order;
        return 0;
    }

    private FlexAlignItems GetEffectiveAlignItems(Control child)
    {
        if (child.TryGetStyleProperty(StylePropertyAlignItems, out FlexAlignItems align))
            return align;
        return AlignItems;
    }

    private sealed class FlexLine
    {
        public List<FlexItem> Items { get; } = new();
        public float MainSize { get; private set; }
        public float CrossSize { get; private set; }
        public float CrossPosition { get; set; }
        public int LineIndex { get; set; }

        public void AddItem(Control control, float mainSize, float actualMainSize, float crossSize)
        {
            Items.Add(new(control, mainSize, actualMainSize, crossSize));
            MainSize += mainSize;
            CrossSize = Math.Max(CrossSize, crossSize);
        }
    }

    private sealed class FlexItem
    {
        public FlexItem(Control control, float mainSize, float actualMainSize, float crossSize)
        {
            Control = control;
            MainSize = mainSize;
            ActualMainSize = actualMainSize;
            CrossSize = crossSize;
        }

        public Control Control { get; }
        public float MainSize { get; }
        public float ActualMainSize { get; }
        public float CrossSize { get; }
    }
}
