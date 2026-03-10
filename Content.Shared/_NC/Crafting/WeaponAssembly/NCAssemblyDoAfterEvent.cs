using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._NC.Crafting.WeaponAssembly;

/// <summary>
/// Ивент для прогрессбара (DoAfter) при сборке деталей на чертеже (Этап 3).
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NCAssemblyDoAfterEvent : SimpleDoAfterEvent
{
}
