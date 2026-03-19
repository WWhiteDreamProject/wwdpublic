using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Позволяет сущности (MobHuman) получать и хранить киберимпланты.
///     Управляет контейнером имплантов и отслеживает занятые слоты.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberwareComponent : Component
{
    public const string ContainerName = "cyberware_implants";

    /// <summary>
    ///     Словарь установленных имплантов: Слот -> Uid Импланта
    /// </summary>
    [DataField("installedImplants")]
    [AutoNetworkedField]
    public Dictionary<CyberwareSlot, EntityUid> InstalledImplants = new();
}