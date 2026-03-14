using Content.Shared._NC.Forensics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Client._NC.Forensics;

public sealed class NcpdForensicsConsoleWindow : DefaultWindow
{
    public event Action<int, NcpdForensicsAlertAction>? OnAlertAction;
    private readonly BoxContainer _list;

    public NcpdForensicsConsoleWindow()
    {
        Title = "NCPD Forensics Alerts";
        MinSize = new Vector2(500f, 400f);

        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 6 };
        Contents.AddChild(root);

        _list = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 4 };
        var scroll = new ScrollContainer { VerticalExpand = true };
        scroll.AddChild(_list);
        root.AddChild(scroll);
    }

    public void UpdateState(NcpdForensicsConsoleBuiState state)
    {
        _list.RemoveAllChildren();
        for (int i = 0; i < state.Alerts.Count; i++)
        {
            var alert = state.Alerts[i];
            var index = i;
            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(5) };
            
            var header = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
            header.AddChild(new Label { Text = $"Victim: {alert.Victim}", FontColorOverride = Color.Yellow, HorizontalExpand = true });
            
            var actions = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 4 };
            var dispatchBtn = new Button { Text = "В Планшет", Modulate = Color.FromHex("#4DD0E1") };
            var printBtn = new Button { Text = "Печать" };

            dispatchBtn.OnPressed += _ => OnAlertAction?.Invoke(index, NcpdForensicsAlertAction.DispatchToTablet);
            printBtn.OnPressed += _ => OnAlertAction?.Invoke(index, NcpdForensicsAlertAction.PrintTicket);

            actions.AddChild(dispatchBtn);
            actions.AddChild(printBtn);
            header.AddChild(actions);

            row.AddChild(header);
            row.AddChild(new Label { Text = $"Location: {alert.Location} ({alert.X:0.0}, {alert.Y:0.0})" });
            row.AddChild(new Label { Text = $"Time: {alert.Time:hh\\:mm\\:ss}" , FontColorOverride = Color.LightSkyBlue});
            row.AddChild(new Control { MinSize = new Vector2(0f, 4f) });
            _list.AddChild(row);
        }
    }
}
