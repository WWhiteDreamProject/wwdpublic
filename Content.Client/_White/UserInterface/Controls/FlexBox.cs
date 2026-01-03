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

        var isRowDirection =
            Direction == FlexDirection.Row ||
            Direction == FlexDirection.RowReverse;

        var isWrap = Wrap != FlexWrap.NoWrap;

        var mainGap = isRowDirection ? ActualColumnGap : ActualRowGap;
        var crossGap = isRowDirection ? ActualRowGap : ActualColumnGap;

        var availableMain = isRowDirection ? availableSize.X : availableSize.Y;
        var hasMainConstraint = !float.IsPositiveInfinity(availableMain);

        // Первый проход: измеряем детей без ограничений
        foreach (var child in visibleChildren)
            child.Measure(Vector2.PositiveInfinity);

        var orderedChildren = visibleChildren
            .OrderBy(c => GetFlexOrder(c))
            .ToList();

        var lines = new List<FlexLine>();
        var currentLine = new FlexLine();

        foreach (var child in orderedChildren)
        {
            var mainSize = isRowDirection
                ? child.DesiredSize.X
                : child.DesiredSize.Y;

            var crossSize = isRowDirection
                ? child.DesiredSize.Y
                : child.DesiredSize.X;

            var projectedMain =
                currentLine.Items.Count == 0
                    ? mainSize
                    : currentLine.MainSize + mainGap + mainSize;

            if (isWrap &&
                hasMainConstraint &&
                projectedMain > availableMain &&
                currentLine.Items.Count > 0)
            {
                lines.Add(currentLine);
                currentLine = new FlexLine();
            }

            currentLine.AddItem(child, mainSize, mainSize, crossSize);
        }

        if (currentLine.Items.Count > 0)
            lines.Add(currentLine);

        var maxMainSize = 0f;
        var totalCrossSize = 0f;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            maxMainSize = Math.Max(maxMainSize, line.MainSize);
            totalCrossSize += line.CrossSize;

            if (i < lines.Count - 1)
                totalCrossSize += crossGap;
        }

        return isRowDirection
            ? new Vector2(
                hasMainConstraint ? Math.Min(maxMainSize, availableSize.X) : maxMainSize,
                totalCrossSize)
            : new Vector2(
                totalCrossSize,
                hasMainConstraint ? Math.Min(maxMainSize, availableSize.Y) : maxMainSize);
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

        var gap = isRowDirection ? columnGap : rowGap;

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
        if (lines.Count == 0)
            return;

        var containerCrossSize = isRowDirection ? containerSize.Y : containerSize.X;
        var gap = isRowDirection ? ActualRowGap : ActualColumnGap;

        var linesCrossSize = lines.Sum(l => l.CrossSize);
        var totalGap = gap * Math.Max(0, lines.Count - 1);
        var occupiedSize = linesCrossSize + totalGap;

        var freeSpace = containerCrossSize - occupiedSize;

        // align-content: stretch
        if (AlignContent == FlexAlignContent.Stretch && freeSpace > 0)
        {
            var extraPerLine = freeSpace / lines.Count;

            for (int i = 0; i < lines.Count; i++)
                lines[i].CrossSize += extraPerLine;

            freeSpace = 0f;
        }

        freeSpace = Math.Max(0, freeSpace);

        float startOffset = 0f;
        float extraGap = 0f;

        switch (AlignContent)
        {
            case FlexAlignContent.FlexStart:
            case FlexAlignContent.Stretch:
                startOffset = 0f;
                break;

            case FlexAlignContent.FlexEnd:
                startOffset = freeSpace;
                break;

            case FlexAlignContent.Center:
                startOffset = freeSpace / 2f;
                break;

            case FlexAlignContent.SpaceBetween:
                if (lines.Count > 1)
                    extraGap = freeSpace / (lines.Count - 1);
                break;

            case FlexAlignContent.SpaceAround:
                extraGap = freeSpace / lines.Count;
                startOffset = extraGap / 2f;
                break;
        }

        var orderedLines = isWrapReverse
            ? lines.AsEnumerable().Reverse().ToList()
            : lines;

        var currentCrossPos = startOffset;

        for (int i = 0; i < orderedLines.Count; i++)
        {
            var line = orderedLines[i];

            line.CrossPosition = currentCrossPos;
            line.LineIndex = i;

            currentCrossPos += line.CrossSize;

            if (i < orderedLines.Count - 1)
                currentCrossPos += gap + extraGap;
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
        if (line.Items.Count == 0)
            return;

        var mainAxisSize = isRowDirection ? containerSize.X : containerSize.Y;
        var itemGap = isRowDirection ? columnGap : rowGap;

        var items = (isRowReverse || isColumnReverse)
            ? line.Items.AsEnumerable().Reverse().ToList()
            : line.Items;

        var itemsMainSize = items.Sum(i => i.MainSize);
        var totalGap = itemGap * Math.Max(0, items.Count - 1);
        var occupiedSize = itemsMainSize + totalGap;
        var freeSpace = Math.Max(0, mainAxisSize - occupiedSize);

        float startOffset = 0f;
        float extraGap = 0f;

        switch (JustifyContent)
        {
            case FlexJustifyContent.FlexStart:
                startOffset = 0f;
                break;

            case FlexJustifyContent.FlexEnd:
                startOffset = freeSpace;
                break;

            case FlexJustifyContent.Center:
                startOffset = freeSpace / 2f;
                break;

            case FlexJustifyContent.SpaceBetween:
                if (items.Count > 1)
                    extraGap = freeSpace / (items.Count - 1);
                break;

            case FlexJustifyContent.SpaceAround:
                extraGap = freeSpace / items.Count;
                startOffset = extraGap / 2f;
                break;

            case FlexJustifyContent.SpaceEvenly:
                extraGap = freeSpace / (items.Count + 1);
                startOffset = extraGap;
                break;
        }

        var currentMainPos = startOffset;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            var crossPosition = CalculateCrossPosition(
                item.Control,
                line.CrossPosition,
                line.CrossSize,
                isRowDirection);

            ArrangeItem(
                item.Control,
                currentMainPos,
                crossPosition,
                item.MainSize,
                line.CrossSize,
                isRowDirection);

            currentMainPos += item.MainSize;

            if (i < items.Count - 1)
                currentMainPos += itemGap + extraGap;
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
        public float CrossSize { get; set; }
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
