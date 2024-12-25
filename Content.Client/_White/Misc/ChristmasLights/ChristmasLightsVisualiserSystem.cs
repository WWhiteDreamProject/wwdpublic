using Content.Shared._White.Misc.ChristmasLights;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
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



public sealed class ChristmasLightsSystem : SharedChristmasLightsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;


    //[Dependency] private readonly IReflectionManager _reflection = default!; // we have reflection at home

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChristmasLightsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ChristmasLightsComponent, AfterAutoHandleStateEvent>(OnAutoState);
        //SubscribeLocalEvent<ChristmasLightsComponent, GetVerbsEvent<AlternativeVerb>>(OnChristmasLightsAltVerbs);
        SubscribeLocalEvent<ChristmasLightsComponent, GetVerbsEvent<Verb>>(OnChristmasLightsVerbs);

        InitModes();
    }


    // Yes, this gets called for every autoupdate. Yes, i should use appearance system and events instead. No, i will not.
    private void OnAutoState(EntityUid uid, ChristmasLightsComponent comp, AfterAutoHandleStateEvent args)
    {
        var sprite = Comp<SpriteComponent>(uid); // if you're reading this with stack trace pointing to this line, consider the following: https://www.youtube.com/watch?v=CPF2-PpfAfA
        sprite.LayerSetState(ChristmasLightsLayers.Glow1, $"greyscale_first_glow{(comp.LowPower ? "_lp" : "")}");
        sprite.LayerSetState(ChristmasLightsLayers.Glow2, $"greyscale_second_glow{(comp.LowPower ? "_lp" : "")}");
    }
    private void OnInit(EntityUid uid, ChristmasLightsComponent comp, ComponentInit args)
    {
        var sprite = Comp<SpriteComponent>(uid); // see above

        SetLamp1Color(sprite, comp.Color1);
        SetLamp2Color(sprite, comp.Color2);
        sprite.LayerSetState(ChristmasLightsLayers.Glow1, $"greyscale_first_glow{(comp.LowPower ? "_lp" : "")}");
        sprite.LayerSetState(ChristmasLightsLayers.Glow2, $"greyscale_second_glow{(comp.LowPower ? "_lp" : "")}");
        sprite.LayerSetAutoAnimated(ChristmasLightsLayers.Glow1, false); // evil grinch smile
        sprite.LayerSetAutoAnimated(ChristmasLightsLayers.Glow2, false);
    }

    private void OnChristmasLightsAltVerbs(EntityUid uid, ChristmasLightsComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        args.Verbs.Add(
            new AlternativeVerb
            {
                Priority = 3,
                Text = Loc.GetString("christmas-lights-toggle-brightness"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
                ClientExclusive = true,
                CloseMenu = false,
                Act = () => {
                    if (_timing.IsFirstTimePredicted) // i hate the antichrist i hate the antichrist i hate the antichrist
                        RaiseNetworkEvent(new ChangeChristmasLightsBrightnessAttemptEvent(GetNetEntity(args.Target)));
                }
            });
    }

    private void OnChristmasLightsVerbs(EntityUid uid, ChristmasLightsComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!_actionBlocker.CanInteract(args.User, uid) || !_interaction.InRangeUnobstructed(args.User, uid))
            return;

        args.Verbs.Add(
            new Verb
            {
                Priority = 3,
                Text = Loc.GetString("christmas-lights-next-mode"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/group.svg.192dpi.png")),
                ClientExclusive = true,
                CloseMenu = false,
                Act = () => RaiseNetworkEvent(new ChangeChristmasLightsModeAttemptEvent(GetNetEntity(args.Target)))
            });
        args.Verbs.Add(
            new Verb
            {
                Priority = 3,
                Text = Loc.GetString("christmas-lights-toggle-brightness"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
                ClientExclusive = true,
                CloseMenu = false,
                Act = () => RaiseNetworkEvent(new ChangeChristmasLightsBrightnessAttemptEvent(GetNetEntity(args.Target)))
            });
    }





    // Funny shitcode game: everything that gets called in FrameUpdate() must be coded as if it's in a shader.




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



    private void SetLamp1Color(SpriteComponent sprite, Color c) // only for init, LEDs changing colors while off is lame
    {
        sprite.LayerSetColor(ChristmasLightsLayers.Lights1, c.WithAlpha(255));
        sprite.LayerSetColor(ChristmasLightsLayers.Glow1, c);
    }

    private void SetLamp2Color(SpriteComponent sprite, Color c) // only for init, LEDs changing colors while off is lame
    {
        sprite.LayerSetColor(ChristmasLightsLayers.Lights2, c.WithAlpha(255));
        sprite.LayerSetColor(ChristmasLightsLayers.Glow2, c);
    }

#region helper functions
    /// <summary>
    /// https://www.desmos.com/calculator/ckyz8ikijz
    /// Don't ask me why. I won't be able to answer.
    /// </summary>
    /// <param name="x"></param>
    /// <returns>x but with a crippling disability</returns>
    private static float shitsin(float x) => (float) ((8 * MathF.Sin(x) + MathF.Sin(3 * x)) / 14 + 0.5);
    private static float step(float x, int stepsNum) => MathF.Round(x * stepsNum) / stepsNum;
    private static int round(float x) => (int) MathF.Round(x);
    private float curtime => (float) _timing.CurTime.TotalSeconds;
    Color getRainbowColor(float secondsPerCycle, float hueOffset, int steps) => Color.FromHsv(new Vector4(step((curtime + hueOffset) % secondsPerCycle / secondsPerCycle, steps), 1f, 1f, 1f));
    private bool cycle(float seconds) => curtime % (seconds * 2) <= seconds;
    /// <summary>
    ///  will not be kept in sync between clients, but who cares, it is different with every access and is intended to induce epileptic seizures
    /// </summary>
    float random { get { 
            _shift++;
            return (1 + MathF.Sin((_shift+curtime) * 5023.85929f)) * 9491.59902f % 1;
        } }
    int _shift = 0;
#endregion

    public override void FrameUpdate(float frameTime) {
        base.FrameUpdate(frameTime);

        var query = AllEntityQuery<ChristmasLightsComponent, SpriteComponent>();

#if DEBUG
        stopwatch.Restart();
#endif
        while (query.MoveNext(out var comp, out var sprite))
        {
            ApplyMode(comp.mode, comp, sprite);
        }
#if DEBUG
        _lastStopwatch = stopwatch.Elapsed.TotalMilliseconds;
        double shit = _cbuf.Add(_lastStopwatch);
        _cumul += _lastStopwatch;
        _cumul -= shit;
        _averageStopwatch = _cumul / _cbuf.size;
#endif
    }

    private void ApplyMode(string mode, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        if (sprite.TryGetLayer(sprite.LayerMapGet(ChristmasLightsLayers.Glow1), out var layer1) &&
            sprite.TryGetLayer(sprite.LayerMapGet(ChristmasLightsLayers.Glow2), out var layer2))
        {
            layer1.Color = comp.Color1; // in case some mode doesn't change the colors, reset them so they don't get stuck at 0 alpha or something
            layer2.Color = comp.Color2;

            if (modes.TryGetValue(mode, out var deleg))
            {
                deleg(layer1, layer2, comp, sprite);
            }

        }
    }

    delegate void ModeFunc(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite);

    Dictionary<string, ModeFunc> modes = new();

    private void InitModes()
    {
        modes["test1"] = modeTest1;
        modes["test2"] = modeTest2;
        modes["emp"] = modeFubar;
        modes["emp_rainbow"] = modeFubarRainbow;
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
        // for playing with hot reload
    }

    void modeTest2(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        // for playing with hot reload
    }
    void modeFubar(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = round(random*3);
        layer2.AnimationFrame = round(random*3);
        layer1.State = $"greyscale_first_glow{(random > 0.5 ? "_lp" : "")}";
        layer2.State = $"greyscale_second_glow{(random > 0.5 ? "_lp" : "")}";
    }

    void modeFubarRainbow(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = round(random * 3);
        layer2.AnimationFrame = round(random * 3);
        layer1.Color = getRainbowColor(1, random, 16);
        layer2.Color = getRainbowColor(1, random, 16);
        layer1.State = $"greyscale_first_glow{(random > 0.5 ? "_lp" : "")}";
        layer2.State = $"greyscale_second_glow{(random > 0.5 ? "_lp" : "")}";
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
        layer1.Color = getRainbowColor(2f, 0f, 10);
        layer2.Color = getRainbowColor(2f, 0.5f, 10);
    }
    void modeSquareWave(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        layer1.AnimationFrame = 3 * round((1+MathF.Sin(curtime))/2);
        layer2.AnimationFrame = 3 * round((1+MathF.Sin(curtime + MathF.PI/2))/2);
    }

    void modeSquareWaveRainbow(SpriteComponent.Layer layer1, SpriteComponent.Layer layer2, ChristmasLightsComponent comp, SpriteComponent sprite)
    {
        modeSquareWave(layer1, layer2, comp, sprite);
        layer1.Color = getRainbowColor(2f, 0f, 10);
        layer2.Color = getRainbowColor(2f, 0.5f, 10);
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
        layer1.Color = getRainbowColor(2f, 0f, 10);
        layer2.Color = getRainbowColor(2f, 0.5f, 10);
    }
}
