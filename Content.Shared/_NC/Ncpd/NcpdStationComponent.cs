using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._NC.Ncpd;

/// <summary>
/// Компонент, который вешается на станцию для хранения логов NCPD и списка отстраненных офицеров.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NcpdStationComponent : Component
{
    /// <summary>
    /// Логи штрафов
    /// </summary>
    [DataField("logs")]
    public List<NcpdLogEntry> Logs = new();

    /// <summary>
    /// Список EntityUid отстраненных сотрудников (у которых забрали значок).
    /// </summary>
    [DataField("suspendedOfficers")]
    public HashSet<EntityUid> SuspendedOfficers = new();
}
