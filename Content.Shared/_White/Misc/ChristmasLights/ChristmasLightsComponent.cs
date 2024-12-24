using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Misc.ChristmasLights;

[RegisterComponent, AutoGenerateComponentState(true), NetworkedComponent]
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

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int CurrentModeIndex = default;

    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<string> modes = new List<string> { "always_on" };

    /// <summary>
    /// refers to the glow state sprites, no actual power consumtion regardless of value
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LowPower = true;

    /// <summary>
    /// as in, are the LEDs capable of changing colors.
    /// Doesn't actually limit anything, only used by server side system to tell
    /// whether it should apply regular or rainbow epilepsy mode when EMP'd.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Multicolor = false;

    [DataField]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");
}

public enum ChristmasLightsLayers
{
    Base,
    Lights1,
    Lights2,
    Glow1,
    Glow2
}

