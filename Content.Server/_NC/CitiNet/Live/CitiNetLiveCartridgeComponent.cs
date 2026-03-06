// NC — Компонент картриджа CitiNet Live.
// Хранит привязку зрителя к просматриваемой камере.

namespace Content.Server._NC.CitiNet.Live;

[RegisterComponent]
public sealed partial class CitiNetLiveCartridgeComponent : Component
{
    /// <summary>EntityUid камеры, которую этот клиент сейчас смотрит.</summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? WatchedCamUid;
}
