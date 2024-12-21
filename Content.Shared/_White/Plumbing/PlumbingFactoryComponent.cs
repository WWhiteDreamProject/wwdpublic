using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Plumbing;

[RegisterComponent]
public sealed partial class PlumbingFactoryComponent : Component
{
    [DataField("reagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string reagentprototype = "Water";
    [DataField]
    public FixedPoint2 perTick = 5;

}

[RegisterComponent]
public sealed partial class PlumbingStorageTankComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "plumbing_storage_tank";

}

[RegisterComponent]
public sealed partial class PlumbingSimpleInputComponent : Component
{
    /// <summary>
    ///  if false, will not accept any reagents.
    /// </summary>
    [DataField]
    public bool Enabled = true;
    /// <summary>
    /// If false, <see cref="ReagentFilters"/> will act as a blacklist.
    /// </summary>
    [DataField]
    public bool Whitelist = false;
    [DataField]
    public List<string> ReagentFilters = new();
    [DataField]
    public string Solution = "plumbing_storage_tank";
    //[DataField]
    //public FixedPoint2 InputPerSecond = 10;
}

[RegisterComponent]
public sealed partial class PlumbingSimpleOutputComponent : Component
{
    /// <summary>
    /// If false, will not send any reagents.
    /// </summary>
    [DataField]
    public bool Enabled = true;
    [DataField]
    public string Solution = "plumbing_storage_tank";
    [DataField]
    public FixedPoint2 OutputPerSecond = 10;
}


[RegisterComponent]
public sealed partial class PlumbingInternalTankComponent : Component
{
    [DataField]
    public string Solution = "plumbing_storage_tank";
}



[RegisterComponent]
public sealed partial class PlumbingItemSlotTankComponent : Component
{
    [DataField(required: true)]
    public string Slot = "";
}

[RegisterComponent]
public sealed partial class PlumbingInfiniteTankComponent : Component
{
    [DataField("reagentId", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>), required: true)]
    public string ReagentPrototype;
    [DataField]
    public FixedPoint2 tankSize = 1;
}


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PlumbingInputOutputVisualiserComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Direction InputDir = Direction.Invalid;
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Direction OutputDir = Direction.Invalid;

    [ViewVariables(VVAccess.ReadWrite)]
    public Angle InputAngleDiff = default;
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle OutputAngleDiff = default;

}

[RegisterComponent]
public sealed partial class PlumbingSeparatorComponent : Component
{
    // ???
}



[RegisterComponent]
public sealed partial class PlumbingPipeVisComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("node", required: true)]
    public string Node;
}
