using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Misc.ChristmasLights;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class ChristmasLightsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color1 = new Color(255, 0, 0);
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color2 = new Color(0, 0, 255);

    /// <summary>
    /// Consult <see cref="Content.Client._White.Misc.ChristmasLights.ChristmasLightsVisualiserSystem"/> for available modes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string mode = "always_on";

    [DataField, AutoNetworkedField]
    public List<string> modes = new List<string>
    {
        "always_on",
        //"sinwave_full",
        //"sinwave_partial",
        //"sinwave_partial_rainbow",
        //"rainbow",
        //"strobe_double",
        //"strobe",
        //"strobe_slow",
    };

}



public enum ChristmasLightsLayers
{
    Base,
    Lights1,
    Lights2,
    Glow1,
    Glow2
}
