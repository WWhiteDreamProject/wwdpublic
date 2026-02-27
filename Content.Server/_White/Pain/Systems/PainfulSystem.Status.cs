using Content.Shared._White.Pain;
using Content.Shared._White.Pain.Components;
using Content.Shared.Chat;
using Robust.Shared.Utility;

namespace Content.Server._White.Pain.Systems;

public sealed partial class PainfulSystem
{
    private void InitializeStatus()
    {
        SubscribeLocalEvent<PainStatusComponent, CheckPainStatusAlertEvent>(OnCheckPainStatusAlert);
    }

    #region Event Handling

    private void OnCheckPainStatusAlert(Entity<PainStatusComponent> ent, ref CheckPainStatusAlertEvent args)
    {
        if (!_player.TryGetSessionByEntity(ent, out var session))
            return;

        var formatted = new FormattedMessage();
        formatted.AddMarkupOrThrow(Loc.GetString("pain-status-alert-check"));

        foreach (var (location, level) in ent.Comp.PainStatus)
        {
            if (level is PainLevel.None or PainLevel.Zero)
                continue;

            formatted.PushNewline();
            formatted.AddMarkupOrThrow(Loc.GetString($"pain-status-alert-check-{location}-{level}".ToLower()));
        }

        if (formatted.Count == 1)
            formatted.AddMarkupOrThrow(Loc.GetString("pain-status-alert-check-empty"));

        var message = formatted.ToMarkup();

        _chat.ChatMessageToOne(ChatChannel.Visual, message, message, ent, false, session.Channel);
    }

    #endregion
}
