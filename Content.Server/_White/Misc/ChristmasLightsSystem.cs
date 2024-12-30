using Content.Server.Emp;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._White.Misc.ChristmasLights;
using Content.Shared.ActionBlocker;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._White.Misc;


public sealed class ChristmasLightsSystem : SharedChristmasLightsSystem
{
    [Dependency] private readonly NodeGroupSystem _node = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
 
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChristmasLightsComponent, ComponentInit>(OnChristmasLightsInit);
        SubscribeLocalEvent<ChristmasLightsComponent, EmpPulseEvent>(OnChristmasLightsMinisculeTrolling);
        SubscribeLocalEvent<ChristmasLightsComponent, GotEmaggedEvent>(OnChristmasLightsModerateTrolling);

        SubscribeNetworkEvent<ChangeChristmasLightsModeAttemptEvent>(OnModeChangeAttempt);
        SubscribeNetworkEvent<ChangeChristmasLightsBrightnessAttemptEvent>(OnBrightnessChangeAttempt);
    }

    private void OnChristmasLightsInit(EntityUid uid, ChristmasLightsComponent comp, ComponentInit args)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var cont))
            return;
        comp.CurrentModeIndex = comp.modes.IndexOf(comp.mode); // returns -1 if mode is not in list: disables mode changing if that's the case
    if (cont.Nodes.TryGetValue("christmaslight", out var node))
        _node.QueueReflood(node);
    }

    private void OnChristmasLightsMinisculeTrolling(EntityUid uid, ChristmasLightsComponent comp, ref EmpPulseEvent args)
    {
        args.Affected = true;

        if (!TryGetConnected(uid, out var nodes))
            return;

        foreach (var node in nodes)
        {
            var jolly = Comp<ChristmasLightsComponent>(node.Owner);
            jolly.mode = $"emp{(comp.Multicolor ? "_rainbow" : "")}";
            Dirty(node.Owner, jolly);
        }
    }

    private void OnChristmasLightsModerateTrolling(EntityUid uid, ChristmasLightsComponent comp, ref GotEmaggedEvent args)
    {
        if (TryGetConnected(uid, out var nodes))
        {
            foreach (var node in nodes)
            {
                EnsureComp<EmaggedComponent>(node.Owner);
                var jolly = Comp<ChristmasLightsComponent>(node.Owner);
                jolly.mode = $"emp{(jolly.Multicolor ? "_rainbow" : "")}";
                jolly.CurrentModeIndex = -1; // disables mode change
                Dirty(jolly.Owner, jolly);
            }
        }
        _audio.PlayPvs(comp.EmagSound, uid);
        args.Handled = true;
    }

    private int GetNextModeIndex(ChristmasLightsComponent comp) // cycles modes as usual, but also handles the -1 case
    {
        if (comp.CurrentModeIndex == -1)
            return -1;

        comp.CurrentModeIndex = (comp.CurrentModeIndex + 1) % comp.modes.Count;
        return comp.CurrentModeIndex;
    }

    private void OnModeChangeAttempt(ChangeChristmasLightsModeAttemptEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (sessionArgs.SenderSession.AttachedEntity is not { } user)
            return;

        var uid = GetEntity(args.target);
        if (!TryComp<ChristmasLightsComponent>(uid, out var thisComp) || !CanInteract(uid, user))
            return;

        _audio.PlayPredicted(thisComp.ButtonSound, uid, user);
        _interaction.DoContactInteraction(user, uid);

        if (HasComp<EmaggedComponent>(uid))
            return;
        
        var jolly = Comp<ChristmasLightsComponent>(uid);
        UpdateAllConnected(uid, jolly.LowPower, GetNextModeIndex(jolly));
    }

    private void OnBrightnessChangeAttempt(ChangeChristmasLightsBrightnessAttemptEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (sessionArgs.SenderSession.AttachedEntity is not { } user)
            return;

        var uid = GetEntity(args.target);
        if (!TryComp<ChristmasLightsComponent>(uid, out var thisComp) || !CanInteract(uid, user))
            return;

        _audio.PlayPredicted(thisComp.ButtonSound, uid, user);
        _interaction.DoContactInteraction(user, uid);

        if (HasComp<EmaggedComponent>(uid))
            return;
        
        var jolly = Comp<ChristmasLightsComponent>(uid);
        UpdateAllConnected(uid, !jolly.LowPower, jolly.CurrentModeIndex);
    }

    /// <summary>
    /// note: also updates the uid passed, so technically it's "UpdateAllConnectedAndItself" or something like that
    /// </summary>
    private void UpdateAllConnected(EntityUid uid, bool brightness, int newModeIndex)
    {
        if (newModeIndex < 0 || !TryGetConnected(uid, out var nodes))
            return;
        
        foreach (var node in nodes)
        {
            var jollyUid = node.Owner;
            if (!TryComp<ChristmasLightsComponent>(jollyUid, out var jolly)
                || HasComp<EmaggedComponent>(jollyUid))
                continue;
            
            jolly.LowPower = brightness;
            jolly.mode = jolly.modes[newModeIndex];
            Dirty(jollyUid, jolly);
        }
    }

    /// <summary>
    /// returns connected *and* self.
    /// </summary>
    private bool TryGetConnected(EntityUid uid, [NotNullWhen(true)] out IEnumerable<Node>? nodes)
    {
        nodes = null;
        
        if (!TryComp<NodeContainerComponent>(uid, out var cont)
            || !cont.Nodes.TryGetValue("christmaslight", out var node)
            || node.NodeGroup is null)
            return false;
        
        nodes = node.NodeGroup.Nodes;
        return true;
    }
}
