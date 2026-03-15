using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Ncpd
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class NcpdTabletComponent : Component
    {
        [DataField]
        public int? ActiveCallId;
    }
}
