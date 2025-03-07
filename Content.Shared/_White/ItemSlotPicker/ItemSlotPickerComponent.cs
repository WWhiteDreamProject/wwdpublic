using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.ItemSlotPicker;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemSlotPickerComponent : Component
{
    [DataField]
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<string> ItemSlots = new();
}
