using Content.Shared._White.Misc.ChristmasLights;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.Commands.GameTiming;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;

//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.Misc.ChristmasLights;

public sealed class ChristmasLightsVisualiserSystem : VisualizerSystem<ChristmasLightsComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

    //[Dependency] private readonly IReflectionManager _reflection = default!; // we have reflection at home

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChristmasLightsComponent, ComponentInit>(OnInit);
        //SubscribeLocalEvent<T, AppearanceChangeEvent>(OnAppearanceChange);

        InitModes();
    }

    //protected virtual void OnAppearanceChange(EntityUid uid, T component, ref AppearanceChangeEvent args) { }

    private void OnInit(EntityUid uid, ChristmasLightsComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return; // anti-dumbass protection

        SetLamp1Color(sprite, comp.Color1);
        SetLamp2Color(sprite, comp.Color2);
        sprite.LayerSetAutoAnimated(ChristmasLightsLayers.Glow1, false); // evil grinch smile
        sprite.LayerSetAutoAnimated(ChristmasLightsLayers.Glow2, false);

        //ApplyMode("always_on", comp, sprite);

    }


    // paranoidal profiling, not for live
#if DEBUG
    class circlebuf
    {
        public readonly int size;
        public circlebuf(int size)
        {
            this.size = size;
            buf = new double[size];
        }
        public double[] buf;
        int i = -1;
        public double Add(double x)
        {
            int ind = (++i) % size; // this takes me back to the indecencies i've used to do in cpp
            double val = buf[ind];
            buf[ind] = x;
            return val;
        }
    }
    // schizo rambling
    circlebuf _cbuf = new(256);
    [ViewVariables]
    double _cumul = 0;
    [ViewVariables, UsedImplicitly] // just for vv
    double _averageStopwatch = 0;
    [ViewVariables]
    double _lastStopwatch = 0;
    Stopwatch stopwatch = new();
#endif


    /// <summary>
    /// https://www.desmos.com/calculator/ckyz8ikijz
    /// Don't ask me why. I won't be able to answer.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>

    private void SetLamp1Color(SpriteComponent sprite, Color c)
    {
        sprite.LayerSetColor(ChristmasLightsLayers.Lights1, c.WithAlpha(255));
        sprite.LayerSetColor(ChristmasLightsLayers.Glow1, c);
    }

    private void SetLamp2Color(SpriteComponent sprite, Color c)
    {
        sprite.LayerSetColor(ChristmasLightsLayers.Lights2, c.WithAlpha(255));
        sprite.LayerSetColor(ChristmasLightsLayers.Glow2, c);
    }

    private static float shitsin(float x) => (float) ((8 * MathF.Sin(x) + MathF.Sin(3 * x)) / 14 + 0.5);
    //private static float shitsin(double x) => shitsin((float) x);
    private static float step(float x, int stepsNum) => MathF.Round(x * stepsNum) / stepsNum;
    private static int round(float x) => (int) MathF.Round(x);
    private float curtime => (float) _timing.CurTime.TotalSeconds;
    Color getRainbowColor(float secondsPerCycle, float hueOffset, int steps) => Color.FromHsv(new Vector4(step((curtime + hueOffset) % secondsPerCycle / secondsPerCycle, steps), 1f, 1f, 1f));
    private bool cycle(float seconds) => curtime % (seconds * 2) <= seconds;

    public override void FrameUpdate(float frameTime) {
        base.FrameUpdate(frameTime);

        //float time1 = shitsin(curtime);
        //float time2 = shitsin(curtime + Math.PI);

        var query = AllEntityQuery<ChristmasLightsComponent, SpriteComponent>();
#if DEBUG
        stopwatch.Restart();
#endif
        while (query.MoveNext(out var comp, out var sprite))
        {
            ApplyMode(comp.mode, /*time1, time2,*/ comp, sprite);
        }
#if DEBUG
        _lastStopwatch = stopwatch.Elapsed.TotalMilliseconds;
        double shit = _cbuf.Add(_lastStopwatch);
        _cumul += _lastStopwatch;
        _cumul -= shit;
        _averageStopwatch = _cumul / _cbuf.size;
#endif
    }

    private void ApplyMode(string mode, /*float time1, float time2,*/ ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        if (sprite.TryGetLayer(sprite.LayerMapGet(ChristmasLightsLayers.Glow1), out var layer1) &&
            sprite.TryGetLayer(sprite.LayerMapGet(ChristmasLightsLayers.Glow2), out var layer2))
        {
            layer1.Color = comp.Color1; // in case some mode doesn't change the colors, reset them so they don't get stuck at 0 alpha or something
            layer2.Color = comp.Color2;


            if (modes.TryGetValue(mode, out var deleg))
            {
                deleg(/*time1, time2,*/ layer1, layer2, comp, sprite);
                //continue;
            }

        }
    }


    // shit like this should be put in a shader methinks...
    delegate void ModeFunc(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite);

    Dictionary<string, ModeFunc> modes = new();

    private void InitModes()
    {
        modes["test1"] = modeTest1;
        modes["test2"] = modeTest2;
        modes["always_on"] = modeAlwaysOn;
        modes["fade"] = modeFade;
        modes["sinwave_full"] = modeSinWaveFull;
        modes["sinwave_partial"] = modeSinWavePartial;
        modes["sinwave_partial_rainbow"] = modeSinWavePartialRainbow;
        modes["squarewave"] = modeSquareWave;
        modes["squarewave_rainbow"] = modeSquareWaveRainbow;
        modes["strobe_double"] = modeStrobeDouble;
        modes["strobe"] = modeStrobe;
        modes["strobe_slow"] = modeStrobeSlow;
        modes["rainbow"] = modeRainbow;
    }

    void modeTest1(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        
    }

    void modeTest2(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {

    }

    void modeAlwaysOn(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = 3;
        layer2.AnimationFrame = 3;
    }
    void modeFade(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        if (cycle(2*MathF.PI))
        {
            layer1.AnimationFrame = round(3 * shitsin(curtime - MathF.PI/2)); // it's important for shitsin to be equal to zero
            layer2.AnimationFrame = 0;                                      // at the beginning of sim for correct color switching.
        }
        else
        {
            layer1.AnimationFrame = 0;
            layer2.AnimationFrame = round(3 * shitsin(curtime - MathF.PI/2));
        }
    }

    void modeSinWaveFull(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = round(3 * shitsin(curtime));
        layer2.AnimationFrame = round(3 * shitsin(curtime + MathF.PI));
    }

    void modeSinWavePartial(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = 1 + round(2 * shitsin(1.33f * curtime));
        layer2.AnimationFrame = 1 + round(2 * shitsin(1.33f * curtime + MathF.PI));
    }

    void modeSinWavePartialRainbow(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        modeSinWavePartial(layer1, layer2, comp, sprite);
        SetLamp1Color(sprite, getRainbowColor(2f, 0f, 10));
        SetLamp2Color(sprite, getRainbowColor(2f, 0.5f, 10));
    }
    void modeSquareWave(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = 3 * round(shitsin(curtime));
        layer2.AnimationFrame = 3 * round(shitsin(curtime + MathF.PI));
    }

    void modeSquareWaveRainbow(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        modeSquareWave(layer1, layer2, comp, sprite);
        SetLamp1Color(sprite, getRainbowColor(2f, 0f, 10));
        SetLamp2Color(sprite, getRainbowColor(2f, 0.5f, 10));
    }

    void modeStrobeDouble(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        float second = curtime % 1;
        int frame = (second % 0.25) < 0.125 ? 3 : 0;
        layer1.AnimationFrame = second < 0.5 ? frame : 0;
        layer2.AnimationFrame = second >= 0.5 ? frame : 0;
    }

    void modeStrobe(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        int frame = (curtime % 0.25) < 0.125 ? 3 : 0;
        layer1.AnimationFrame = frame;
        layer2.AnimationFrame = 3 - frame;
    }

    void modeStrobeSlow(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        float second = curtime % 1;
        layer1.AnimationFrame = second < 0.5 ? 3 : 0;
        layer2.AnimationFrame = second >= 0.5 ? 3 : 0;
    }
    void modeRainbow(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = 3;
        layer2.AnimationFrame = 3;
        SetLamp1Color(sprite, getRainbowColor(2f, 0f, 10));
        SetLamp2Color(sprite, getRainbowColor(2f, 0.5f, 10));
    }
}
