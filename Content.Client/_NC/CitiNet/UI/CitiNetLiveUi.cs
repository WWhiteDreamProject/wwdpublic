// NC — Клиентский UI-контроллер картриджа CitiNet Live.
// Связывает XAML-фрагмент с BUI-сообщениями.

using Content.Client.Eye;
using Content.Client.UserInterface.Fragments;
using Content.Shared._NC.CitiNet;
using Content.Shared._NC.CitiNet.Live;
using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client._NC.CitiNet.UI;

/// <summary>
/// NC — UIFragment для отдельного картриджа CitiNet Live.
/// </summary>
public sealed partial class CitiNetLiveUi : UIFragment
{
    private CitiNetLiveUiFragment? _fragment;
    private int _lastChatCount;
    private EntityUid? _currentCamera;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CitiNetLiveUiFragment();

        // Подписка на Live-действия (StartStream, StopStream, WatchStream, etc.)
        _fragment.OnSendLiveMessage += (type, content) =>
        {
            var liveMessage = new CitiNetLiveMessageEvent(type, content);
            var message = new CartridgeUiMessage(liveMessage);
            userInterface.SendMessage(message);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CitiNetLiveUiState cast || _fragment == null)
            return;

        // Звук на новое сообщение/донат
        var chatCount = cast.ChatMessages.Count;
        if (chatCount > _lastChatCount)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var audio = entManager.System<SharedAudioSystem>();

            var lastMsg = cast.ChatMessages[^1];
            if (lastMsg.IsSystem && lastMsg.Sender == Loc.GetString("citinet-live-donate-chat-prefix"))
                audio.PlayGlobal("/Audio/Effects/coinpull.ogg", Filter.Local(), false);
            else
                audio.PlayGlobal("/Audio/Machines/chime.ogg", Filter.Local(), false);
        }
        _lastChatCount = chatCount;

        // Обновляем Eye камеры если смотрим стрим
        if (cast.WatchedCamNetEntity.HasValue)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var cam = entManager.GetEntity(cast.WatchedCamNetEntity.Value);

            if (_currentCamera != cam)
            {
                var eyeLerp = entManager.System<EyeLerpingSystem>();

                // Убрать предыдущий Eye
                if (_currentCamera != null && entManager.EntityExists(_currentCamera.Value))
                    eyeLerp.RemoveEye(_currentCamera.Value);

                // Добавить новый Eye
                if (entManager.EntityExists(cam))
                {
                    eyeLerp.AddEye(cam);
                    if (entManager.TryGetComponent<EyeComponent>(cam, out var eyeComp))
                        _fragment.SetCameraView(eyeComp.Eye);
                    else
                        _fragment.SetCameraView(null);
                }
                _currentCamera = cam;
            }
        }
        else if (_currentCamera != null)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var eyeLerp = entManager.System<EyeLerpingSystem>();
            if (entManager.EntityExists(_currentCamera.Value))
                eyeLerp.RemoveEye(_currentCamera.Value);
            _currentCamera = null;
            _fragment.SetCameraView(null);
        }

        _fragment.UpdateState(cast);
    }
}
