using Content.Client.Message;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;


namespace Content.Client._NC.Trade.Controls;


public sealed class NcStoreHeaderBar : BoxContainer
{
    private const int DefaultSearchDebounceMs = 120;
    private readonly RichTextLabel _balanceInfo;

    private readonly TextureRect _currencyIcon;

    private readonly Dictionary<string, Texture> _currencyIconCache = new();
    private readonly LineEdit _searchBar;
    private IPrototypeManager? _proto;

    private int _searchToken;
    private SpriteSystem? _sprites;

    public NcStoreHeaderBar()
    {
        Orientation = LayoutOrientation.Horizontal;

        _currencyIcon = new()
        {
            MinSize = new(24, 24),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Margin = new(4, 0, 4, 0),
            VerticalAlignment = VAlignment.Center
        };

        _balanceInfo = new()
        {
            HorizontalExpand = false,
            VerticalAlignment = VAlignment.Center
        };
        _balanceInfo.AddStyleClass("LabelHeading");

        _searchBar = new()
        {
            HorizontalExpand = false,
            MinWidth = 250,
            Access = AccessLevel.Public
        };

        AddChild(_currencyIcon);
        AddChild(_balanceInfo);
        AddChild(new() { HorizontalExpand = true, });
        AddChild(new Label { Text = "🔍", Margin = new(0, 0, 4, 0), VerticalAlignment = VAlignment.Center, });
        AddChild(_searchBar);

        _balanceInfo.SetMarkup("[font size=14][color=yellow]0[/color][/font]");

        _searchBar.OnTextChanged += _ => HandleSearchTextChanged();
    }

    public event Action<string>? OnSearchChanged;

    /// <summary>
    ///     Optional: bind services for currency icon resolution.
    ///     Call once from NcStoreMenu after it resolved its dependencies.
    /// </summary>
    public void BindServices(IPrototypeManager proto, SpriteSystem sprites)
    {
        _proto = proto;
        _sprites = sprites;
        _currencyIconCache.Clear();
    }

    public void SetSearchText(string text) => _searchBar.Text = text;

    public string GetSearchText() => _searchBar.Text;

    public void SetBalances(IReadOnlyDictionary<string, int> balancesByCurrency)
    {
        if (balancesByCurrency.Count == 0)
        {
            SetBalanceMarkup(0);
            _currencyIcon.Texture = null;
            return;
        }

        string? displayCurrency = null;
        var displayAmount = 0;

        if (balancesByCurrency.Count == 1)
        {
            foreach (var (cur, amt) in balancesByCurrency)
            {
                displayCurrency = cur;
                displayAmount = amt;
                break;
            }
        }
        else
        {
            string? bestKey = null;
            var bestValue = int.MinValue;

            foreach (var (key, value) in balancesByCurrency)
            {
                displayAmount += value;
                if (bestKey == null || value > bestValue || value == bestValue &&
                    string.CompareOrdinal(key, bestKey) < 0)
                {
                    bestKey = key;
                    bestValue = value;
                }
            }

            displayCurrency = bestKey;
        }

        SetBalanceMarkup(displayAmount);
        SetCurrencyIcon(displayCurrency);
    }

    private void SetBalanceMarkup(int amount) =>
        _balanceInfo.SetMarkup($"[font size=14][color=yellow]{amount}[/color][/font]");

    private void SetCurrencyIcon(string? currencyId)
    {
        if (string.IsNullOrWhiteSpace(currencyId) || _proto == null || _sprites == null)
        {
            _currencyIcon.Texture = null;
            return;
        }

        if (_currencyIconCache.TryGetValue(currencyId, out var cached))
        {
            _currencyIcon.Texture = cached;
            return;
        }

        if (_proto.TryIndex<StackPrototype>(currencyId, out var stackProto) &&
            _proto.TryIndex<EntityPrototype>(stackProto.Spawn, out var entProto))
        {
            var tex = _sprites.GetPrototypeIcon(entProto).Default;
            _currencyIconCache[currencyId] = tex;
            _currencyIcon.Texture = tex;
            return;
        }

        _currencyIcon.Texture = null;
    }

    private void HandleSearchTextChanged()
    {
        var token = ++_searchToken;
        var text = _searchBar.Text.Trim();

        Timer.Spawn(
            TimeSpan.FromMilliseconds(DefaultSearchDebounceMs),
            () =>
            {
                if (token != _searchToken)
                    return;

                OnSearchChanged?.Invoke(text);
            });
    }
}
