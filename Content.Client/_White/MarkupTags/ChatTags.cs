using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.MarkupTags;

// Chat font tags

[UsedImplicitly]
public sealed class ChatBoldTag : IMarkupTag
{
    public const string BoldFont = "ChatBold";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "cbold";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager,
            context.Tags.Any(static x => x is ChatItalicTag || x is ShortChatItalicTag)
                ? ChatBoldItalicTag.BoldItalicFont
                : BoldFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

[UsedImplicitly]
public sealed class ChatItalicTag : IMarkupTag
{
    public const string ItalicFont = "ChatItalic";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "citalic";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager,
            context.Tags.Any(static x => x is ChatBoldTag || x is ShortChatBoldTag)
                ? ChatBoldItalicTag.BoldItalicFont
                : ItalicFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

[UsedImplicitly]
public sealed class ChatBoldItalicTag : IMarkupTag
{
    public const string BoldItalicFont = "ChatBoldItalic";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "cbolditalic";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager, BoldItalicFont);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

// Shorthands

[UsedImplicitly]
public sealed class ShortBoldTag : IMarkupTag
{
    public const string BoldFont = "DefaultBold";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "b";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager,
            context.Tags.Any(static x => x is ItalicTag || x is ShortItalicTag)
                ? ShortBoldItalicTag.BoldItalicFont
                : BoldFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

[UsedImplicitly]
public sealed class ShortItalicTag : IMarkupTag
{
    public const string ItalicFont = "DefaultItalic";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "i";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager,
            context.Tags.Any(static x => x is BoldTag || x is ShortBoldTag)
                ? ShortBoldItalicTag.BoldItalicFont
                : ItalicFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

[UsedImplicitly]
public sealed class ShortBoldItalicTag : IMarkupTag
{
    public const string BoldItalicFont = "DefaultBoldItalic";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "bi";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager, BoldItalicFont);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

// Chat font shorthands

[UsedImplicitly]
public sealed class ShortChatBoldTag : IMarkupTag
{
    public static string BoldFont => ChatBoldTag.BoldFont;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "cb";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager,
            context.Tags.Any(static x => x is ChatItalicTag || x is ShortChatItalicTag)
                ? ChatBoldItalicTag.BoldItalicFont
                : BoldFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

[UsedImplicitly]
public sealed class ShortChatItalicTag : IMarkupTag
{
    public static string ItalicFont = ChatItalicTag.ItalicFont;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "ci";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager,
            context.Tags.Any(static x => x is ChatBoldTag || x is ShortChatBoldTag)
                ? ChatBoldItalicTag.BoldItalicFont
                : ItalicFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

[UsedImplicitly]
public sealed class ShortChatBoldItalicTag : IMarkupTag
{
    public static string BoldItalicFont => ChatBoldItalicTag.BoldItalicFont;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "cbi";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager, BoldItalicFont);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

