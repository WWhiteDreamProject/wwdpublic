using Content.Client.UserInterface.Fragments;
using Content.Shared._NC.CitiNet;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;


namespace Content.Client._NC.CitiNet.UI;

/// <summary>
/// NC — Клиентский UI-контроллер картриджа CitiNet.
/// Связывает UI-фрагмент с BUI-сообщениями.
/// </summary>
public sealed partial class CitiNetUi : UIFragment
{
    private CitiNetUiFragment? _fragment;

    private int _lastCallMessagesCount = 0;
    private int _lastGroupMessagesCount = 0;
    private int _lastBbsMessagesCount = 0;
    private CitiNetCallState _lastCallState = CitiNetCallState.None;


    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    private void PlayNotificationSound(string path)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var audio = entManager.System<SharedAudioSystem>();

        // Воспроизводим звук локально для клиента
        audio.PlayGlobal(path, Filter.Local(), false);
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CitiNetUiFragment();

        // Подписка на обычные действия CitiNet
        _fragment.OnSendMessage += (type, targetId, content) =>
        {
            var citiNetMessage = new CitiNetUiMessageEvent(type, targetId, content);
            var message = new CartridgeUiMessage(citiNetMessage);
            userInterface.SendMessage(message);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CitiNetUiState cast) return;

        // --- SOUND LOGIC ---
        bool playMessageSound = false;

        if (cast.CallMessages.Count > _lastCallMessagesCount) playMessageSound = true;
        if (cast.GroupMessages.Count > _lastGroupMessagesCount) playMessageSound = true;
        if (cast.ChannelMessages.Count > _lastBbsMessagesCount) playMessageSound = true;

        if (playMessageSound)
            PlayNotificationSound("/Audio/Machines/chime.ogg");

        // Звук входящего вызова
        if (cast.CallState == CitiNetCallState.Incoming && _lastCallState != CitiNetCallState.Incoming)
            PlayNotificationSound("/Audio/Machines/double_ring.ogg");

        _lastCallMessagesCount = cast.CallMessages.Count;
        _lastGroupMessagesCount = cast.GroupMessages.Count;
        _lastBbsMessagesCount = cast.ChannelMessages.Count;
        _lastCallState = cast.CallState;


        _fragment?.UpdateState(cast);
    }
}
