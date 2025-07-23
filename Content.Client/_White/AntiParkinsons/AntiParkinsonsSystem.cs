using Content.Client.Interaction;
using Content.Client.Outline;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Reflection;
using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Shared._White.CCVar;
using Content.Shared._White.Move;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Client._White.AntiParkinsons;

public sealed class AntiParkinsonsSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _refl = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    bool _enabled = false;
    bool _doingShit = false;
    Vector2 _savedLocalPos;
    EntityUid _modifiedEntity = EntityUid.Invalid;
    Vector2 _modifiedLocalPos;

    public delegate void MoveEventHandlerProxy(ref MoveEventProxy ev);
    public event MoveEventHandlerProxy? OnGlobalMoveEvent;


    public override void Initialize()
    {
        UpdatesOutsidePrediction = true;
        _cfg.OnValueChanged(WhiteCVars.PixelSnapCamera, OnEnabledDisabled, true);
        UpdatesBefore.Add(typeof(EyeSystem));   // so that EyeSystem moves the eye to the spessman insead of moving it ourselves
        UpdatesBefore.Add(typeof(AudioSystem)); // the rest is stuff that also updates after EyeSystem.
        UpdatesBefore.Add(typeof(MidiSystem));  // Without this, the system update order fails to resolve.
        UpdatesBefore.Add(typeof(InteractionOutlineSystem));
        UpdatesBefore.Add(typeof(DragDropSystem));

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
        if (!_enabled || !_doingShit)
        {
            var evproxy = new MoveEventProxy(ev.Entity, ev.OldPosition, ev.NewPosition, ev.OldRotation, ev.NewRotation);
            RaiseLocalEvent(ev.Sender, ref evproxy);
            OnGlobalMoveEvent?.Invoke(ref evproxy);
        }
    }

    private void OnPlayerAttached(EntityUid ent)
    {

    }

    private void OnPlayerDetached(EntityUid ent)
    {

    }

    private void OnEnabledDisabled(bool val)
    {
        _enabled = val;
    }

    public override void FrameUpdate(float frameTime)
    {

        if (_player.LocalEntity is not EntityUid localEnt || !TryComp<TransformComponent>(localEnt, out var xform))
            return;

        if (!_enabled)
            return;

        _modifiedEntity = localEnt;
        var iterXform = xform;
        while (!TerminatingOrDeleted(xform.ParentUid) &&
            !HasComp<MapGridComponent>(xform.ParentUid) &&
            !HasComp<MapComponent>(xform.ParentUid))
            xform = Transform(xform.ParentUid);

        _modifiedEntity = xform.Owner;
        _savedLocalPos = xform.LocalPosition;
        _modifiedLocalPos = _savedLocalPos;

        _doingShit = true;
        // i really want to make sure that _doingShit doesn't get stuck on true for even a single frame,
        // as that would result in ALL moveEvents being dropped on client.
        try
        {
            const int roundFactor = EyeManager.PixelsPerMeter * 2;
            _modifiedLocalPos = RoundVec(_savedLocalPos, roundFactor);
            xform.LocalPosition = _modifiedLocalPos;
            _eye.CurrentEye.Offset = RoundVec(_eye.CurrentEye.Offset, roundFactor);
        }
        catch (Exception e) { throw; }
        finally
        {
            _doingShit = false;
        }
    }

    public void FrameUpdateRevert()
    {
        if (!_enabled || _player.LocalEntity is not EntityUid localEnt || !TryComp<TransformComponent>(_modifiedEntity, out var modifiedXform))
            return;

        // if this is true, then our localpos was updated outside the system Update() loop,
        // probably after a server state was applied. In that case, keep the new value
        // instead of reverting to the old one.
        if (modifiedXform.LocalPosition != _modifiedLocalPos)
            return;

        _doingShit = true;
        try
        {
            modifiedXform.LocalPosition = _savedLocalPos;
        }
        catch (Exception e) { throw; }
        finally
        {
            _doingShit = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector2 RoundVec(Vector2 vec, float roundFactor) => Vector2.Round(vec * roundFactor) / roundFactor;
}

public sealed class AntiParkinsonsRevertSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _refl = default!;
    [Dependency] private readonly AntiParkinsonsSystem _parkinsons = default!;


    public override void Initialize()
    {
        UpdatesOutsidePrediction = true;

        foreach (Type sys in _refl.GetAllChildren<EntitySystem>())
        {
            if (sys.IsAbstract || sys == typeof(AntiParkinsonsRevertSystem))
                continue;

            UpdatesBefore.Add(sys);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        _parkinsons.FrameUpdateRevert();
    }
}
