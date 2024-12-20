using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Plumbing;

[RegisterComponent]
public partial class PlumbingFactoryComponent : Component
{
    [DataField("reagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string reagentprototype = "Water";
    [DataField]
    public FixedPoint2 perTick = 5;

}

[RegisterComponent]
public partial class PlumbingStorageTankComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "plumbing_storage_tank";
}

[RegisterComponent]
public partial class PlumbingSimpleInputComponent : Component
{
    [DataField]
    public bool UseFilters = false;
    [DataField]
    public List<string> ReagentFilters = new();
    [DataField]
    public string Solution = "plumbing_storage_tank";
    //[DataField]
    //public FixedPoint2 InputPerSecond = 10;
}

[RegisterComponent]
public partial class PlumbingSimpleOutputComponent : Component
{
    [DataField]
    public string Solution = "plumbing_storage_tank";
    [DataField]
    public FixedPoint2 OutputPerSecond = 10;
}


[RegisterComponent]
public partial class PlumbingInternalTankComponent : Component
{
    [DataField]
    public string Solution = "plumbing_storage_tank";
}



[RegisterComponent]
public partial class PlumbingItemSlotTankComponent : Component
{
    [DataField(required: true)]
    public string Slot = "";
}


