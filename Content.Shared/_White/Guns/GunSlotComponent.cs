using Content.Shared.Containers.ItemSlots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns;

[RegisterComponent]
public sealed partial class GunSlotComponent : Component
{
    [DataField(required: true)]
    public string Slot = "";
}
