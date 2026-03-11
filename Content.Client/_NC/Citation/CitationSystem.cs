using Content.Shared._NC.Citation;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Network;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Client._NC.Citation;

/// <summary>
/// Клиентская система для обработки входящих штрафов (окно жертвы).
/// </summary>
public sealed class CitationSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CitationTargetUiMessage>(OnTargetUiMessage);
    }

    private void OnTargetUiMessage(CitationTargetUiMessage ev)
    {
        // Создаем угрожающее уведомление с красной рамкой
        var window = new DefaultWindow()
        {
            Title = "ВНИМАНИЕ: ШТРАФ NCPD",
            MinSize = new Vector2(350, 150),
        };
        // window.Stylesheet = ... стилизация под киберпанк/грубый терминал

        var vbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10),
            SeparationOverride = 10
        };

        var label = new Label
        {
            Text = $"Офицер {ev.OfficerName} выписал вам штраф.\nСумма: {ev.Amount} Эдди.\nПричина: {ev.Reason}",
            FontColorOverride = Color.FromHex("#ffb700"), // янтарный монохром
            HorizontalAlignment = Control.HAlignment.Center,
        };

        var hbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = Control.HAlignment.Center,
            SeparationOverride = 20
        };

        var acceptButton = new Button
        {
            Text = "ОПЛАТИТЬ (Приложить ID)",
            Modulate = Color.LimeGreen
        };
        acceptButton.OnPressed += _ =>
        {
            RaiseNetworkEvent(new CitationTargetResponseMessage(ev.TerminalUid, true));
            window.Close();
        };

        var declineButton = new Button
        {
            Text = "ОТКАЗАТЬСЯ",
            Modulate = Color.Red
        };
        declineButton.OnPressed += _ =>
        {
            RaiseNetworkEvent(new CitationTargetResponseMessage(ev.TerminalUid, false));
            window.Close();
        };

        hbox.AddChild(acceptButton);
        hbox.AddChild(declineButton);

        vbox.AddChild(label);
        vbox.AddChild(hbox);

        window.Contents.AddChild(vbox);

        _uiManager.WindowRoot.AddChild(window);
        window.OpenCentered();
    }
}
