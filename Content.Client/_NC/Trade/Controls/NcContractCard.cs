using Content.Shared._NC.Trade;
using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;


namespace Content.Client._NC.Trade.Controls;


public sealed class NcContractCard : PanelContainer
{
    private readonly ContractClientData _data;
    private readonly IPrototypeManager _proto;
    private readonly SpriteSystem _sprites;
    private readonly IEntityManager _entMan;
    private const int TargetIconPx = 96;
    private const int RewardIconPx = 40;


    public NcContractCard(ContractClientData data, IPrototypeManager protoMan, SpriteSystem sprites, IEntityManager entMan)
    {
        _data = data;
        _proto = protoMan;
        _sprites = sprites;
        _entMan = entMan;

        HorizontalExpand = true;
        Margin = new(4, 0, 4, 8);

        BuildUi();
    }

    public event Action<string>? OnClaim;

    private void BuildUi()
    {
        var borderColor = DifficultyColor(_data.Difficulty, _data.Completed);

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };
        AddChild(row);

        var diffStrip = new PanelContainer
        {
            MinSize = new(4, 0),
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat { BackgroundColor = borderColor, },
            Margin = new(0, 0, 6, 0)
        };
        row.AddChild(diffStrip);

        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new(0.06f, 0.06f, 0.07f, 0.98f),
                BorderColor = borderColor,
                BorderThickness = new(2),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6
            }
        };
        row.AddChild(panel);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };
        panel.AddChild(root);

        root.AddChild(BuildHeader(borderColor));

        var descText = BuildPrettyDescription(_data);
        if (!string.IsNullOrWhiteSpace(descText))
        {
            root.AddChild(
                new Label
                {
                    Text = descText,
                    Margin = new(0, 0, 0, 8),
                    Modulate = Color.FromHex("#C9C9C9")
                });
        }

        root.AddChild(
            new Label
            {
                Text = Loc.GetString("nc-store-contract-goals-header"),
                Margin = new(0, 0, 0, 2),
                Modulate = Color.FromHex("#8A8A8A")
            });

        if (_data.Targets is { Count: > 0 })
        {
            foreach (var t in _data.Targets)
                root.AddChild(BuildTargetRow(t.TargetItem, t.Required));
        }
        else
            root.AddChild(BuildTargetRow(_data.TargetItem, _data.Required));

        if (!_data.Completed)
        {
            var max = CalculateRequiredTotal(_data);
            var val = Math.Clamp(_data.Progress, 0, max);

            var progressLabel = new Label
            {
                Text = Loc.GetString("nc-store-contract-progress-line", ("progress", val), ("required", max)),
                Margin = new(0, 6, 0, 2),
                Align = Label.AlignMode.Right
            };
            progressLabel.StyleClasses.Add("LabelSubText");
            root.AddChild(progressLabel);

            root.AddChild(
                new ProgressBar
                {
                    MinValue = 0,
                    MaxValue = max,
                    Value = val,
                    HorizontalExpand = true,
                    MinSize = new(0, 10),
                    Margin = new(0, 0, 0, 4)
                });
        }

        root.AddChild(BuildBottom());
    }

    private Control BuildHeader(Color borderColor)
    {
        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new(0, 0, 0, 4)
        };

        var titleLabel = new Label
        {
            Text = BuildPrettyTitle(_data),
            Margin = new(0, 0, 6, 0),
            HorizontalExpand = true
        };
        titleLabel.StyleClasses.Add("LabelHeading");
        header.AddChild(titleLabel);

        header.AddChild(new() { HorizontalExpand = true, });

        if (!_data.Repeatable)
        {
            var tip = Loc.GetString("nc-store-contract-badge-single-tooltip");
            header.AddChild(
                BuildBadge(
                    Loc.GetString("nc-store-contract-badge-single"),
                    tip,
                    new(0.12f, 0.12f, 0.14f),
                    new(0f, 0f, 0f, 0.7f)));
        }

        if (_data.Completed)
        {
            header.AddChild(
                BuildBadge(
                    Loc.GetString("nc-store-contract-badge-completed"),
                    Loc.GetString("nc-store-contract-badge-completed-tooltip"),
                    Color.FromHex("#1E3A1E"),
                    Color.FromHex("#4CAF50")));
        }

        return header;
    }

    private static PanelContainer BuildBadge(string text, string? tooltip, Color bg, Color border)
    {
        var badge = new PanelContainer
        {
            VerticalAlignment = VAlignment.Center,
            Margin = new(0, 1, 6, 0),
            MouseFilter = MouseFilterMode.Stop,
            ToolTip = tooltip,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = bg,
                BorderColor = border,
                BorderThickness = new(1),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
                ContentMarginTopOverride = 2,
                ContentMarginBottomOverride = 2
            }
        };

        var badgeText = new Label
        {
            Text = text,
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
            ToolTip = tooltip
        };
        badgeText.StyleClasses.Add("LabelSubText");

        badge.AddChild(badgeText);
        return badge;
    }

    private Control BuildBottom()
    {
        var bottomWrap = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new(0, 6, 0, 0)
        };

        var rewardsPanel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new(0.05f, 0.05f, 0.06f, 0.6f),
                BorderColor = new(0f, 0f, 0f, 0.55f),
                BorderThickness = new(1),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6
            }
        };

        var rewardsCol = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };
        rewardsPanel.AddChild(rewardsCol);

        var rewardsHeader = new Label
        {
            Text = Loc.GetString("nc-store-contract-reward-header"),
            Margin = new(0, 0, 0, 3)
        };
        rewardsHeader.StyleClasses.Add("LabelHeading");
        rewardsCol.AddChild(rewardsHeader);

        PopulateRewards(rewardsCol, _data.Rewards);
        bottomWrap.AddChild(rewardsPanel);

        var actionCol = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinSize = new(180, 0),
            Margin = new(8, 0, 0, 0)
        };

        var canClaim = _data.Completed;

        var actionHint = new Label
        {
            Text = canClaim
                ? Loc.GetString("nc-store-contract-action-can-claim")
                : Loc.GetString("nc-store-contract-action-not-done"),
            Margin = new(0, 0, 0, 4),
            Align = Label.AlignMode.Center
        };
        actionHint.StyleClasses.Add("LabelSubText");
        actionCol.AddChild(actionHint);

        actionCol.AddChild(new() { VerticalExpand = true, });

        var btn = new Button
        {
            Text = canClaim
                ? Loc.GetString("nc-store-contract-action-claim")
                : Loc.GetString(
                    "nc-store-contract-action-claim-progress",
                    ("progress", _data.Progress),
                    ("required", CalculateRequiredTotal(_data))),
            Disabled = !canClaim,
            HorizontalExpand = true,
            MinSize = new(0, 32)
        };

        if (canClaim)
            btn.Modulate = Color.FromHex("#4CAF50");

        btn.ToolTip = canClaim
            ? !_data.Repeatable
                ? Loc.GetString("nc-store-contract-claim-tooltip-single")
                : Loc.GetString("nc-store-contract-claim-tooltip-repeatable")
            : Loc.GetString("nc-store-contract-claim-tooltip-not-done");

        btn.OnPressed += _ =>
        {
            if (!canClaim)
                return;

            OnClaim?.Invoke(_data.Id);
        };

        actionCol.AddChild(btn);
        bottomWrap.AddChild(actionCol);

        return bottomWrap;
    }

    // =====================
    // Text helpers
    // =====================

    private string BuildPrettyTitle(ContractClientData c)
    {
        if (!string.IsNullOrWhiteSpace(c.Name))
            return c.Name.Trim();

        var diff = DifficultyName(c.Difficulty);
        var goal = BuildGoalsInline(c, 2);

        return string.IsNullOrWhiteSpace(goal)
            ? Loc.GetString("nc-store-contract-title-pretty-nogoal", ("difficulty", diff))
            : Loc.GetString("nc-store-contract-title-pretty", ("difficulty", diff), ("goal", goal));
    }

    private string BuildPrettyDescription(ContractClientData c)
    {
        if (!string.IsNullOrWhiteSpace(c.Description))
            return c.Description.Trim();

        var goal = BuildGoalsInline(c, 4);
        if (string.IsNullOrWhiteSpace(goal))
            return Loc.GetString("nc-store-contract-desc-default");

        return Loc.GetString("nc-store-contract-desc-generated", ("goals", goal.Replace(", ", "; ")));
    }

    private string BuildGoalsInline(ContractClientData c, int maxParts)
    {
        var parts = new List<string>(maxParts);

        if (c.Targets is { Count: > 0 })
        {
            foreach (var t in c.Targets)
            {
                if (parts.Count >= maxParts)
                    break;

                if (t.Required <= 0 || string.IsNullOrWhiteSpace(t.TargetItem))
                    continue;

                var name = ResolveProtoName(t.TargetItem);
                parts.Add(Loc.GetString("nc-store-contract-goal-inline", ("item", name), ("count", t.Required)));
            }
        }
        else
        {
            if (c.Required > 0 && !string.IsNullOrWhiteSpace(c.TargetItem))
            {
                var name = ResolveProtoName(c.TargetItem);
                parts.Add(Loc.GetString("nc-store-contract-goal-inline", ("item", name), ("count", c.Required)));
            }
        }

        return string.Join(", ", parts);
    }

    private int CalculateRequiredTotal(ContractClientData c)
    {
        if (c.Targets is { Count: > 0 })
        {
            var sum = 0;
            foreach (var t in c.Targets)
                if (t.Required > 0)
                    sum += t.Required;

            return Math.Max(1, sum);
        }

        return Math.Max(1, c.Required);
    }

    // =====================
    // Targets / tooltips
    // =====================

    private Control BuildTargetRow(string? protoId, int required)
    {
        EntityPrototype? targetProto = null;
        if (!string.IsNullOrWhiteSpace(protoId))
            _proto.TryIndex(protoId, out targetProto);

        var targetRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new(0, 0, 0, 2),
            MouseFilter = MouseFilterMode.Stop
        };

        var tooltip = BuildProtoTooltip(targetProto);
        if (!string.IsNullOrWhiteSpace(tooltip))
            targetRow.ToolTip = tooltip;

        if (!string.IsNullOrWhiteSpace(protoId))
        {
            var view = new EntityPrototypeView
            {
                MinSize = new(TargetIconPx, TargetIconPx),
                MaxSize = new(TargetIconPx, TargetIconPx),
                Margin = new(0, 0, 4, 0),
                MouseFilter = MouseFilterMode.Ignore
            };
            view.SetPrototype(protoId);
            NcUiIconFit.Fit(view, _sprites, protoId, targetPx: TargetIconPx, paddingPx: 4);
            targetRow.AddChild(view);
        }

        var targetName = targetProto?.Name ?? protoId ?? Loc.GetString("nc-store-unknown-item");
        targetRow.AddChild(
            new Label
            {
                Text = Loc.GetString("nc-store-contract-goal-line", ("item", targetName), ("count", required)),
                MouseFilter = MouseFilterMode.Ignore
            });

        return targetRow;
    }

    private string ResolveProtoName(string protoId)
    {
        if (_proto.TryIndex<EntityPrototype>(protoId, out var proto))
            return proto.Name;

        return protoId;
    }

    private static string BuildProtoTooltip(EntityPrototype? proto)
    {
        if (proto == null)
            return string.Empty;

        if (string.IsNullOrWhiteSpace(proto.Description))
            return Loc.GetString("nc-store-proto-tooltip-name-only", ("name", proto.Name));

        return Loc.GetString("nc-store-proto-tooltip", ("name", proto.Name), ("desc", proto.Description));
    }

    // =====================
    // Rewards
    // =====================

    private void PopulateRewards(BoxContainer rewardsCol, List<ContractRewardData>? rewards)
    {
        if (rewards is not { Count: > 0, })
        {
            rewardsCol.AddChild(
                new Label
                {
                    Text = Loc.GetString("nc-store-contract-reward-none"),
                    Modulate = Color.FromHex("#777777")
                });
            return;
        }

        var currencyTotals = new Dictionary<string, int>();
        var itemTotals = new Dictionary<string, int>();

        foreach (var r in rewards)
        {
            if (r.Amount <= 0 || string.IsNullOrWhiteSpace(r.Id))
                continue;

            switch (r.Type)
            {
                case StoreRewardType.Currency:
                    if (!currencyTotals.TryAdd(r.Id, r.Amount))
                        currencyTotals[r.Id] += r.Amount;
                    break;

                case StoreRewardType.Item:
                    if (!itemTotals.TryAdd(r.Id, r.Amount))
                        itemTotals[r.Id] += r.Amount;
                    break;

                case StoreRewardType.Pool:
                    break;
            }
        }

        if (currencyTotals.Count > 0)
        {
            var parts = new List<string>(currencyTotals.Count);
            foreach (var kv in currencyTotals)
            {
                var name = CurrencyName(kv.Key);
                if (string.IsNullOrWhiteSpace(name))
                    name = kv.Key;

                parts.Add(Loc.GetString("nc-store-currency-format", ("amount", kv.Value), ("currency", name)));
            }

            rewardsCol.AddChild(
                new Label
                {
                    Text = string.Join(", ", parts),
                    Modulate = Color.FromHex("#D4AF37")
                });
        }

        if (itemTotals.Count > 0)
        {
            if (currencyTotals.Count > 0)
                rewardsCol.AddChild(new() { MinSize = new(0, 4), });

            foreach (var kv in itemTotals)
            {
                var id = kv.Key;
                var count = kv.Value;
                if (count <= 0 || string.IsNullOrWhiteSpace(id))
                    continue;

                _proto.TryIndex<EntityPrototype>(id, out var proto);

                var line = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Margin = new(0, 0, 0, 2),
                    MouseFilter = MouseFilterMode.Stop
                };

                var tooltip = BuildProtoTooltip(proto);
                if (!string.IsNullOrWhiteSpace(tooltip))
                    line.ToolTip = tooltip;

                if (!string.IsNullOrWhiteSpace(id))
                {
                    var view = new EntityPrototypeView
                    {
                        MinSize = new(RewardIconPx, RewardIconPx),
                        MaxSize = new(RewardIconPx, RewardIconPx),
                        Margin = new(0, 0, 4, 0),
                        MouseFilter = MouseFilterMode.Ignore
                    };
                    view.SetPrototype(id);
                    NcUiIconFit.Fit(view, _sprites, id, targetPx: RewardIconPx, paddingPx: 0, mul: 1.25f, variant: 1);
                    line.AddChild(view);
                }

                var name = proto?.Name ?? id;
                line.AddChild(
                    new Label
                    {
                        Text = Loc.GetString("nc-store-contract-reward-item-line", ("item", name), ("count", count)),
                        MouseFilter = MouseFilterMode.Ignore
                    });

                rewardsCol.AddChild(line);
            }
        }

        if (currencyTotals.Count == 0 && itemTotals.Count == 0)
        {
            rewardsCol.AddChild(
                new Label
                {
                    Text = Loc.GetString("nc-store-contract-reward-none"),
                    Modulate = Color.FromHex("#777777")
                });
        }
    }

    private string CurrencyName(string? currencyId)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
            return string.Empty;

        if (_proto.TryIndex<StackPrototype>(currencyId, out var stackProto) &&
            _proto.TryIndex<EntityPrototype>(stackProto.Spawn, out var currencyEnt))
            return currencyEnt.Name;

        return currencyId;
    }

    // =====================
    // Difficulty
    // =====================

    private Color DifficultyColor(string diff, bool completed)
    {
        var baseColor = diff switch
        {
            "Easy" => Color.FromHex("#4CAF50"),
            "Medium" => Color.FromHex("#FFC107"),
            "Hard" => Color.FromHex("#F44336"),
            _ => Color.FromHex("#9E9E9E")
        };

        return completed ? Brighten(baseColor, 0.7f) : baseColor;
    }

    private string DifficultyName(string diff) =>
        diff switch
        {
            "Easy" => Loc.GetString("nc-store-difficulty-easy"),
            "Medium" => Loc.GetString("nc-store-difficulty-medium"),
            "Hard" => Loc.GetString("nc-store-difficulty-hard"),
            _ => diff
        };

    private static Color Brighten(Color c, float f) =>
        new(MathF.Min(c.R * f, 1f), MathF.Min(c.G * f, 1f), MathF.Min(c.B * f, 1f), c.A);
}
