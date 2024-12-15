using Content.Shared.Chemistry.Reagent;
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
    public int perTick = 5;

}

[RegisterComponent]
public partial class PlumbingStorageTankComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "storage_tank";
}

[RegisterComponent]
public partial class PlumbingSimpleDemandComponent : Component
{
    [DataField(required: true)]
    public string Solution = default!;


}

