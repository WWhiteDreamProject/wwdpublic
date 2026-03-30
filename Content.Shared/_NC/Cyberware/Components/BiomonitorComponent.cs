using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Ручной биомонитор для запуска нейро-терапии.
/// </summary>
[RegisterComponent]
public sealed partial class BiomonitorComponent : Component
{
    /// <summary>
    ///     Радиус прослушивания локального чата (в тайлах).
    /// </summary>
    [DataField("listenRange")]
    public float ListenRange = 4f;

    /// <summary>
    ///     Положительный отклик (лечащее слово).
    /// </summary>
    [DataField("successSound")]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Effects/alert.ogg");

    /// <summary>
    ///     Сигнал при попадании в пул (подтверждение направления).
    /// </summary>
    [DataField("pingSound")]
    public SoundSpecifier PingSound = new SoundPathSpecifier("/Audio/Effects/empulse.ogg");

    /// <summary>
    ///     Сигнал при болевом триггере.
    /// </summary>
    [DataField("traumaSound")]
    public SoundSpecifier TraumaSound = new SoundPathSpecifier("/Audio/Effects/Arcade/error.ogg");

    /// <summary>
    ///     Текущий подключённый пациент.
    /// </summary>
    [ViewVariables]
    public EntityUid? ConnectedPatient;
}
