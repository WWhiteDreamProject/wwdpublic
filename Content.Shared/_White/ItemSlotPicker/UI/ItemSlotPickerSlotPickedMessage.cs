using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.ItemSlotPicker.UI;

[Serializable, NetSerializable]
public sealed class ItemSlotPickerSlotPickedMessage(string id) : BoundUserInterfaceMessage
{
    public string SlotId = id;
}


//[Serializable, NetSerializable]
public sealed class ItemSlotPickerContentsChangedMessage() : BoundUserInterfaceMessage
{
}
