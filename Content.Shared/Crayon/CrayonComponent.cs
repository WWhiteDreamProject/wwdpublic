using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Crayon
{

    // WWDP EDIT START
    /// <summary>
    /// Component holding the state of a crayon-like component
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
    public sealed partial class CrayonComponent : Component
    {
        /// <summary>
        /// The ID of currently selected decal prototype that will be placed when the crayon is used
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public string SelectedState = string.Empty;

        /// <summary>
        /// Color with which the crayon will draw
        /// </summary>
        [DataField]
        [AutoNetworkedField]
        public Color Color;


        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public Angle Angle;

        [DataField]
        [AutoNetworkedField]
        public SoundSpecifier? UseSound;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        [AutoNetworkedField]
        public bool SelectableColor;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public int Charges;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        [AutoNetworkedField]
        public int Capacity = 30;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        [AutoNetworkedField]
        public bool DeleteEmpty = true;

        /// <summary>
        /// Used clientside only.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UIUpdateNeeded;
        // WWDP EDIT END

        [Serializable, NetSerializable]
        public enum CrayonUiKey : byte
        {
            Key,
        }
    }

    /// <summary>
    /// Used by the client to notify the server about the selected decal ID
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonSelectMessage : BoundUserInterfaceMessage
    {
        public readonly string State;
        public CrayonSelectMessage(string selected)
        {
            State = selected;
        }
    }

    /// <summary>
    /// Sets the color of the crayon, used by Rainbow Crayon
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonColorMessage : BoundUserInterfaceMessage
    {
        public readonly Color Color;
        public CrayonColorMessage(Color color)
        {
            Color = color;
        }
    }

    /// <summary>
    /// Server to CLIENT. Notifies the BUI that a decal with given ID has been drawn.
    /// Allows the client UI to advance forward in the client-only ephemeral queue,
    /// preventing the crayon from becoming a magic text storage device.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonUsedMessage : BoundUserInterfaceMessage
    {
        public readonly string DrawnDecal;

        public CrayonUsedMessage(string drawn)
        {
            DrawnDecal = drawn;
        }
    }

    /* // WWDP EDIT - DEFUNCT - Moved to using AutoState system.
    /// <summary>
    /// Component state, describes how many charges are left in the crayon in the near-hand UI
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonComponentState : ComponentState
    {
        public readonly Color Color;
        public readonly string State;
        public readonly int Charges;
        public readonly int Capacity;
        public readonly Angle Angle;

        public CrayonComponentState(Color color, string state, int charges, int capacity, Angle angle)
        {
            Color = color;
            State = state;
            Charges = charges;
            Capacity = capacity;
            Angle = angle;
        }
    }
    */

    /// <summary>
    /// The state of the crayon UI as sent by the server
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CrayonBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string Selected;
        /// <summary>
        /// Whether or not the color can be selected
        /// </summary>
        public bool SelectableColor;
        public Color Color;

        public CrayonBoundUserInterfaceState(string selected, bool selectableColor, Color color)
        {
            Selected = selected;
            SelectableColor = selectableColor;
            Color = color;
        }
    }
}
