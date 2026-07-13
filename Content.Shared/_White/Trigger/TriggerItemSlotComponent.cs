using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Trigger;

[RegisterComponent]
public sealed partial class TriggerItemSlotComponent : Component
{
    [DataField(required: true)]
    public List<string> Slots = new();
}
