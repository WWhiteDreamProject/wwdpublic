using Content.Server.Chat.Systems;
using Content.Server.VoiceMask;
using Content.Shared.Clumsy;
using Content.Shared.Interaction.Components;
using Robust.Shared.Network;

namespace Content.Server.Chat;

public sealed class FunnyFontsChatSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
    }

    private void OnTransformSpeech(TransformSpeechEvent ev)
    {
        if (TryComp(ev.Sender, out VoiceMaskComponent? mask) && mask.VoiceMaskName != null)
            return;

        if (TryComp<ClumsyComponent>(ev.Sender, out _))
        {
            ev.Message = $"[font=\"ComicSansMS\"]{ev.Message}[/font]";
        }
    }
}
