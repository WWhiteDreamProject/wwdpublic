using Content.Shared._NC.Cyberware.Components;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;

namespace Content.Server._NC.Cyberware.Systems;

/// <summary>
///     Перехватывает речь персонажей с низкой человечностью и имитирует киберпсихоз.
/// </summary>
public sealed class CyberpsychosisSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Подписываемся на глобальное событие трансформации речи
        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
    }

    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!TryComp<HumanityComponent>(args.Sender, out var component))
            return;

        // Если человечность выше 30, речь нормальная
        if (component.CurrentHumanity > 30f)
            return;

        // Чем меньше человечность, тем выше шанс искажения
        float severity = 1f - (component.CurrentHumanity / 30f); // 0.0 до 1.0
        
        if (_random.Prob(severity))
        {
            // Здесь мы меняем сообщение, которое будет отправлено в чат
            args.Message = GarbleMessage(args.Message, severity);
        }
    }

    private string GarbleMessage(string message, float severity)
    {
        var words = message.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (_random.Prob(severity * 0.5f))
            {
                words[i] = _random.Pick(new[] { "...[СБОЙ]...", "...хр-р-р...", "...мясо...", "...хром...", "...убить..." });
            }
        }
        return string.Join(" ", words);
    }
}