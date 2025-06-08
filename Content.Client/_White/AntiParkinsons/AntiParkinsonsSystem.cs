using Content.Client.Interaction;
using Content.Client.Outline;
using Content.Shared._White;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Reflection;
using System.Linq;
using System.Numerics;

namespace Content.Client._White.AntiParkinsons;

// The following code is slightly esoteric and higly schizophrenic. You have been warned.

#pragma warning disable RA0002

public sealed class AntiParkinsonsSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _refl = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public bool Enabled = false;
    [Access(typeof(AntiParkinsonsSystem), typeof(AntiParkinsonsRevertSystem))]
    public bool DoingShit = false;
    [Access(typeof(AntiParkinsonsSystem), typeof(AntiParkinsonsRevertSystem))]
    public Vector2 SavedWorldPos;
    public Vector2 ModifiedWorldPos;
    [Access(typeof(AntiParkinsonsSystem), typeof(AntiParkinsonsRevertSystem))]
    public EntityUid LastParent = EntityUid.Invalid;

    public delegate void MoveEventHandlerProxy(ref MoveEventProxy ev);
    public event MoveEventHandlerProxy? OnGlobalMoveEvent;


    public override void Initialize()
    {
        UpdatesOutsidePrediction = true;
        _cfg.OnValueChanged(WhiteCVars.PixelSnapCamera, OnEnabledDisabled, true);
        UpdatesBefore.Add(typeof(EyeSystem));
        UpdatesBefore.Add(typeof(AudioSystem));
        UpdatesBefore.Add(typeof(MidiSystem));
        UpdatesBefore.Add(typeof(InteractionOutlineSystem));
        UpdatesBefore.Add(typeof(DragDropSystem));

        // eat sand
        foreach (Type sys in _refl.GetAllChildren<EntitySystem>())
        {
            if (sys.IsAbstract || sys == typeof(AntiParkinsonsSystem) || UpdatesBefore.Contains(sys))
                continue;

            UpdatesAfter.Add(sys);
        }


        _player.LocalPlayerAttached += OnPlayerAttached;
        _player.LocalPlayerDetached += OnPlayerDetached;
        _transform.OnGlobalMoveEvent += OnMoveEventGlobal;
    }

    private void OnMoveEventGlobal(ref MoveEvent ev)
    {
        if (!Enabled || !DoingShit)
        {
            var evproxy = new MoveEventProxy(ev.Entity, ev.OldPosition, ev.NewPosition, ev.OldRotation, ev.NewRotation);
            RaiseLocalEvent(ev.Sender, ref evproxy);
            OnGlobalMoveEvent?.Invoke(ref evproxy);
        }
    }

    private void OnPlayerAttached(EntityUid ent)
    {
        LastParent = Transform(ent).ParentUid;
    }

    private void OnPlayerDetached(EntityUid ent)
    {

    }

    private void OnEnabledDisabled(bool val)
    {
        Enabled = val;
    }

    public override void FrameUpdate(float frameTime)
    {

        if (_player.LocalEntity is not EntityUid localEnt || !TryComp<TransformComponent>(localEnt, out var xform))
            return;

        if (!HasComp<MapComponent>(xform.ParentUid))
            LastParent = xform.ParentUid;

        SavedWorldPos = _transform.GetWorldPosition(localEnt);
        ModifiedWorldPos = SavedWorldPos;
        if (!Enabled)
            return;


        DoingShit = true;
        ModifiedWorldPos = PPCamHelper.WorldPosPixelRoundToParent(SavedWorldPos, LastParent, _transform);
        _transform.SetWorldPosition(localEnt, ModifiedWorldPos);
        DoingShit = false;
    }

    public void FrameUpdateRevert()
    {
        if (!Enabled || _player.LocalEntity is not EntityUid localEnt || !TryComp<TransformComponent>(localEnt, out var xform))
            return;

        if (_transform.GetWorldPosition(localEnt) != ModifiedWorldPos)
            return;

        DoingShit = true;
        _transform.SetWorldPosition(localEnt, SavedWorldPos);
        DoingShit = false;
    }
}



public sealed class AntiParkinsonsRevertSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _refl = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AntiParkinsonsSystem _parkinsons = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        UpdatesOutsidePrediction = true;

        // dnas tae
        foreach (Type sys in _refl.GetAllChildren<EntitySystem>())
        {
            if (sys.IsAbstract || sys == typeof(AntiParkinsonsRevertSystem))
                continue;

            UpdatesBefore.Add(sys);
        }
    }

    // dnas tae
    public override void FrameUpdate(float frameTime)
    {
        _parkinsons.FrameUpdateRevert();
    }
}

#pragma warning restore RA0002
