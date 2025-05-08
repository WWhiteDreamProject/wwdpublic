using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Chat;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;


namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorButton : ChatPopupButton<ChannelSelectorPopup>
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    public event Action<ChatSelectChannel>? OnChannelSelect;

    public ChatSelectChannel SelectedChannel { get; private set; }

    private const int SelectorDropdownOffset = 38;

    private TextureRect _textureRect;

    public ChannelSelectorButton()
    {
        IoCManager.InjectDependencies(this);
        Name = "ChannelSelector";

        _textureRect = new TextureRect();
        _textureRect.Visible = false;
        _textureRect.TextureScale = Vector2.One * 2f;
        AddChild(_textureRect);

        Popup.Selected += OnChannelSelected;

        if (Popup.FirstChannel is { } firstSelector)
        {
            Select(firstSelector);
        }
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalLeft = GlobalPosition.X;
        var globalBot = GlobalPosition.Y + Height;
        return UIBox2.FromDimensions(
            new Vector2(globalLeft, globalBot),
            new Vector2(SizeBox.Width, SelectorDropdownOffset));
    }

    private void OnChannelSelected(ChatSelectChannel channel)
    {
        Select(channel);
    }

    public void Select(ChatSelectChannel channel)
    {
        if (Popup.Visible)
        {
            Popup.Close();
        }

        if (SelectedChannel == channel)
            return;
        SelectedChannel = channel;
        OnChannelSelect?.Invoke(channel);
    }

    public static string ChannelSelectorName(ChatSelectChannel channel)
    {
        return Loc.GetString($"hud-chatbox-select-channel-{channel}");
    }

    public Color ChannelSelectColor(ChatSelectChannel channel)
    {
        return channel switch
        {
            ChatSelectChannel.Radio => Color.LimeGreen,
            ChatSelectChannel.LOOC => Color.MediumTurquoise,
            ChatSelectChannel.OOC => Color.LightSkyBlue,
            ChatSelectChannel.Dead => Color.MediumPurple,
            ChatSelectChannel.Admin => Color.HotPink,
            ChatSelectChannel.Telepathic => Color.PaleVioletRed, //Nyano - Summary: determines the color for the chat.
            _ => Color.DarkGray
        };
    }

    public bool TryFindImage(string channel,[NotNullWhen(true)] out Texture? texture)
    {
        if (!_resourceCache.TryGetResource<TextureResource>("/Textures/_White/NovaUI/Light/"+channel+".png", out var textureResource))
        {
            Logger.Debug("Failed /Textures/_White/NovaUI/Light/"+channel+".png");
            texture = null;
            return false;
        }
        Logger.Debug("Success /Textures/_White/NovaUI/Light/"+channel+".png");
        texture = textureResource.Texture;

        return true;
    }

    public void UpdateChannelSelectButton(ChatSelectChannel channel, Shared.Radio.RadioChannelPrototype? radio)
    {
        if (TryFindImage(radio != null ? radio.ID.ToLower() : channel.ToString().ToLower(), out var image))
        {
            Label.Visible = false;
            _textureRect.Visible = true;
            _textureRect.Texture = image;
            StyleBoxOverride = new StyleBoxEmpty();
            return;
        }

        _textureRect.Visible = false;
        Label.Visible = true;
        StyleBoxOverride = null;

        Text = radio != null ? Loc.GetString(radio.Name) : ChannelSelectorName(channel);
        Modulate = radio?.Color ?? ChannelSelectColor(channel);
    }
}
