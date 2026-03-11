using Content.Shared._NC.Forensics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Client._NC.Forensics;

public sealed class NcpdForensicsConsoleWindow : DefaultWindow
{
    private readonly BoxContainer _list;

    public NcpdForensicsConsoleWindow()
    {
        Title = "NCPD Forensics Alerts";
        MinSize = new Vector2(400f, 300f);

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
        foreach (var alert in state.Alerts)
        {
            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
            row.AddChild(new Label { Text = $"Victim: {alert.Victim}", FontColorOverride = Color.Yellow });
            row.AddChild(new Label { Text = $"Location: {alert.Location} ({alert.X:0.0}, {alert.Y:0.0})" });
            row.AddChild(new Label { Text = $"Time: {alert.Time:hh\\:mm\\:ss}" , FontColorOverride = Color.LightSkyBlue});
            row.AddChild(new Control { MinSize = new Vector2(0f, 4f) });
            _list.AddChild(row);
        }
    }
}
