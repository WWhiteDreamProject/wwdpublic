using Content.Shared._NC.CitiNet;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Client._NC.CitiNet;

public sealed class CitiNetNodeWindow : DefaultWindow
{
    public event Action? OnEmergencyExtraction;

    private readonly ProgressBar _downloadProgress;
    private readonly Label _progressText;
    private readonly Label _phaseLabel;
    private readonly Label _statusLabel;
    private readonly Label _overheatWarning;
    private readonly Label _timeLabel;
    private readonly Label _powerLabel;
    private readonly Button _emergencyButton;

    public CitiNetNodeWindow()
    {
        Title = Loc.GetString("citinet-ui-title");
        SetSize = new Vector2(600, 400);

        var neonGreen = Color.FromHex("#39FF14");
        var neonOrange = Color.FromHex("#FFA500");
        var neonRed = Color.FromHex("#FF3131");
        
        var darkBgStyle = new StyleBoxFlat { BackgroundColor = Color.FromHex("#1A1A1A") };

        var rootContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10)
        };

        var header = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
        header.AddChild(new Label { Text = Loc.GetString("citinet-ui-title"), FontColorOverride = neonGreen });
        header.AddChild(new Control { HorizontalExpand = true });
        header.AddChild(new Label { Text = Loc.GetString("citinet-ui-night-city"), FontColorOverride = neonRed });
        rootContainer.AddChild(header);

        var mainPanel = new PanelContainer { VerticalExpand = true, Margin = new Thickness(0, 10, 0, 0) };
        var mainContentLayout = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };

        var statusContainer = new PanelContainer { PanelOverride = darkBgStyle, Margin = new Thickness(0, 0, 0, 10) };
        _statusLabel = new Label { Text = Loc.GetString("citinet-ui-status-idle"), Margin = new Thickness(5), FontColorOverride = neonOrange };
        statusContainer.AddChild(_statusLabel);
        mainContentLayout.AddChild(statusContainer);

        var columns = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, VerticalExpand = true };
        
        var leftCol = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true, Margin = new Thickness(0, 0, 10, 0) };
        _phaseLabel = new Label { Text = Loc.GetString("citinet-ui-phase-idle"), FontColorOverride = neonGreen };
        _downloadProgress = new ProgressBar { MinValue = 0, MaxValue = 1, MinHeight = 40, Margin = new Thickness(0, 5) };
        _progressText = new Label { Text = Loc.GetString("citinet-ui-progress", ("percent", 0)), FontColorOverride = neonOrange };
        
        var warningPanel = new PanelContainer { MinHeight = 60, Margin = new Thickness(0, 10) };
        _overheatWarning = new Label 
        { 
            Text = Loc.GetString("citinet-ui-overheat"), 
            HorizontalAlignment = Control.HAlignment.Center, 
            VerticalAlignment = Control.VAlignment.Center,
            FontColorOverride = neonRed,
            Visible = false
        };
        warningPanel.AddChild(_overheatWarning);
        
        _timeLabel = new Label { Text = Loc.GetString("citinet-ui-time-idle") };

        leftCol.AddChild(_phaseLabel);
        leftCol.AddChild(_downloadProgress);
        leftCol.AddChild(_progressText);
        leftCol.AddChild(warningPanel);
        leftCol.AddChild(_timeLabel);
        columns.AddChild(leftCol);

        var rightCol = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, MinWidth = 180 };
        
        _emergencyButton = new Button { MinHeight = 50 };
        _emergencyButton.AddChild(new Label { 
            Text = Loc.GetString("citinet-ui-btn-emergency"), 
            FontColorOverride = neonRed, 
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center
        });
        _emergencyButton.OnPressed += _ => OnEmergencyExtraction?.Invoke();
        
        var subText = new Label { Text = Loc.GetString("citinet-ui-btn-emergency-desc"), HorizontalAlignment = Control.HAlignment.Center, StyleClasses = { "LabelSmall" } };
        
        var vulnBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(0, 15) };
        vulnBox.AddChild(new Label { Text = Loc.GetString("citinet-ui-vuln-title"), FontColorOverride = neonRed });
        vulnBox.AddChild(new Label { Text = Loc.GetString("citinet-ui-vuln-desc"), StyleClasses = { "LabelSmall" } });

        _powerLabel = new Label { Text = Loc.GetString("citinet-ui-power-stable"), FontColorOverride = neonOrange, Margin = new Thickness(0, 10) };

        var legend = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(0, 5) };
        legend.AddChild(CreateLegendItem(Loc.GetString("citinet-ui-legend-idle"), neonGreen));
        legend.AddChild(CreateLegendItem(Loc.GetString("citinet-ui-legend-downloading"), neonOrange));
        legend.AddChild(CreateLegendItem(Loc.GetString("citinet-ui-legend-cooldown"), neonRed));

        rightCol.AddChild(_emergencyButton);
        rightCol.AddChild(subText);
        rightCol.AddChild(vulnBox);
        rightCol.AddChild(_powerLabel);
        rightCol.AddChild(legend);
        columns.AddChild(rightCol);

        mainContentLayout.AddChild(columns);
        mainPanel.AddChild(mainContentLayout);
        rootContainer.AddChild(mainPanel);
        
        Contents.AddChild(rootContainer);
    }

    private Control CreateLegendItem(string text, Color color)
    {
        var box = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
        box.AddChild(new PanelContainer { 
            PanelOverride = new StyleBoxFlat { BackgroundColor = color }, 
            MinWidth = 10, MinHeight = 10, Margin = new Thickness(2) 
        });
        box.AddChild(new Label { Text = text, StyleClasses = { "LabelSmall" } });
        return box;
    }

    public void UpdateState(CitiNetNodeBoundUserInterfaceState state)
    {
        _downloadProgress.Value = state.Progress;
        _progressText.Text = Loc.GetString("citinet-ui-progress", ("percent", (int)(state.Progress * 100)));
        
        switch (state.State)
        {
            case CitiNetNodeState.Idle:
                _phaseLabel.Text = Loc.GetString("citinet-ui-phase-idle");
                _phaseLabel.FontColorOverride = Color.FromHex("#39FF14");
                _statusLabel.Text = Loc.GetString("citinet-ui-status-idle");
                _overheatWarning.Visible = false;
                _timeLabel.Text = Loc.GetString("citinet-ui-time-idle");
                _emergencyButton.Disabled = true;
                break;
            
            case CitiNetNodeState.Downloading:
                _phaseLabel.Text = Loc.GetString("citinet-ui-phase-downloading");
                _phaseLabel.FontColorOverride = Color.FromHex("#FFA500");
                _statusLabel.Text = Loc.GetString("citinet-ui-status-downloading");
                _emergencyButton.Disabled = false;
                _overheatWarning.Visible = state.Progress > 0.4f;
                
                var remainingSecs = (int)((1.0f - state.Progress) * 240);
                _timeLabel.Text = Loc.GetString("citinet-ui-time-downloading", ("minutes", remainingSecs / 60), ("seconds", remainingSecs % 60));
                break;

            case CitiNetNodeState.Cooldown:
                _phaseLabel.Text = Loc.GetString("citinet-ui-phase-cooldown");
                _phaseLabel.FontColorOverride = Color.FromHex("#FF3131");
                _statusLabel.Text = Loc.GetString("citinet-ui-status-cooldown");
                _overheatWarning.Visible = false;
                _timeLabel.Text = Loc.GetString("citinet-ui-time-cooldown", ("seconds", (int)state.RemainingCooldown));
                _emergencyButton.Disabled = true;
                break;
        }

        if (state.IsPowered)
        {
            _powerLabel.Text = Loc.GetString("citinet-ui-power-stable");
            _powerLabel.FontColorOverride = Color.FromHex("#FFA500");
        }
        else
        {
            _powerLabel.Text = Loc.GetString("citinet-ui-power-fail");
            _powerLabel.FontColorOverride = Color.FromHex("#FF3131");
        }
    }
}
