using Robust.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.DollyMixture;

[RegisterComponent]
public sealed partial class DollyMixtureComponent : Component
{
    [DataField("sprite")]
    public string RSIPath = "";

    [DataField(required: true)]
    public List<string> States = default!;

    public RSI? RSI;

    [DataField]
    public Vector2 LayerOffset = new(0, 1);

    [DataField]
    public Vector2 Offset;

    public Angle LastAngle;
    public List<int> LayerIndices = new();

    [DataField]
    public int RepeatLayers = 1;

    [DataField]
    public Vector2 LayerScale = Vector2.One;

    [DataField]
    public string? DefaultShader;
}
