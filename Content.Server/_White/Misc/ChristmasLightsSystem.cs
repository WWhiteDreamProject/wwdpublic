using Content.Server.Emp;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._White.Misc.ChristmasLights;
using Content.Shared.ActionBlocker;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Misc;


public sealed class ChristmasLightsSystem : SharedChristmasLightsSystem
{
    [Dependency] private readonly NodeGroupSystem _node = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChristmasLightsComponent, ComponentInit>(OnChristmasLightsInit);
        //SubscribeLocalEvent<ChristmasLightsComponent, ActivateInWorldEvent>(OnChristmasLightsActivateInWorld); // functionality moved to verbs
        SubscribeLocalEvent<ChristmasLightsComponent, EmpPulseEvent>(OnChristmasLightsMinisculeTrolling);
        SubscribeLocalEvent<ChristmasLightsComponent, GotEmaggedEvent>(OnChristmasLightsModerateTrolling);

        SubscribeNetworkEvent<ChangeChristmasLightsModeAttemptEvent>(OnModeChangeAttempt);
        SubscribeNetworkEvent<ChangeChristmasLightsBrightnessAttemptEvent>(OnBrightnessChangeAttempt);
    }

    private void OnChristmasLightsInit(EntityUid uid, ChristmasLightsComponent comp, ComponentInit args)
    {
        if (TryComp<NodeContainerComponent>(uid, out var cont))
        {
            comp.CurrentModeIndex = comp.modes.IndexOf(comp.mode); // returns -1 if mode is not in list: disables mode changing if that's the case
            if (cont.Nodes.TryGetValue("christmaslight", out var node))
                _node.QueueReflood(node);
        }
    }

    private void OnChristmasLightsMinisculeTrolling(EntityUid uid, ChristmasLightsComponent comp, ref EmpPulseEvent args)
    {
        args.Affected = true;
        if (TryGetConnected(uid, out var nodes))
        {
            foreach (var node in nodes)
            {
                var jolly = Comp<ChristmasLightsComponent>(node.Owner);
                jolly.mode = $"emp{(comp.Multicolor ? "_rainbow" : "")}";
                Dirty(jolly.Owner, jolly);
            }
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

    int GetNextModeIndex(ChristmasLightsComponent comp) // cycles modes as usual, but also handles the -1 case
    {
        if (comp.CurrentModeIndex == -1) return -1;
        comp.CurrentModeIndex = (comp.CurrentModeIndex + 1) % comp.modes.Count;
        return comp.CurrentModeIndex;
    }

    private void OnModeChangeAttempt(ChangeChristmasLightsModeAttemptEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (!sessionArgs.SenderSession.AttachedEntity.HasValue)
            return;
        EntityUid uid = GetEntity(args.target);
        EntityUid user = sessionArgs.SenderSession.AttachedEntity!.Value; // no it will not be a fucking null, shut the fuck up
        if (_actionBlocker.CanInteract(user, uid) && _interaction.InRangeUnobstructed(user, uid) && !HasComp<EmaggedComponent>(uid))
        {
            var jolly = Comp<ChristmasLightsComponent>(uid);
            UpdateAllConnected(uid, jolly.LowPower, GetNextModeIndex(jolly));
        }

    }

    private void OnBrightnessChangeAttempt(ChangeChristmasLightsBrightnessAttemptEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (!sessionArgs.SenderSession.AttachedEntity.HasValue)
            return;
        EntityUid uid = GetEntity(args.target);
        EntityUid user = sessionArgs.SenderSession.AttachedEntity!.Value; 
        if (_actionBlocker.CanInteract(user, uid) && _interaction.InRangeUnobstructed(user, uid) && !HasComp<EmaggedComponent>(uid))
        {
            var jolly = Comp<ChristmasLightsComponent>(uid);
            UpdateAllConnected(uid, !jolly.LowPower, GetNextModeIndex(jolly));
        }
    }

    private void OnChristmasLightsActivateInWorld(EntityUid uid, ChristmasLightsComponent comp, ActivateInWorldEvent args)
    {
        if (!HasComp<EmaggedComponent>(uid))
        {
            var jolly = Comp<ChristmasLightsComponent>(uid);
            UpdateAllConnected(uid, jolly.LowPower, GetNextModeIndex(jolly));
        }
    }

    /// <summary>
    /// note: also updates the uid passed, so technically it's "UpdateAllConnectedAndItself" or something like that
    /// </summary>
    private void UpdateAllConnected(EntityUid uid, bool brightness, int newModeIndex)
    {
        if (newModeIndex >= 0 && TryGetConnected(uid, out var nodes))
        {
            foreach (var node in nodes)
            {
                var jollyUid = node.Owner;
                if (TryComp<ChristmasLightsComponent>(jollyUid, out var jolly))
                {
                    if (HasComp<EmaggedComponent>(jollyUid))
                        continue;
                    jolly.LowPower = brightness;
                    jolly.mode = jolly.modes[newModeIndex];
                    Dirty(jollyUid, jolly);
                }
            }
        }
    }


    /// <summary>
    /// returns connected *and* self.
    /// </summary>
    private bool TryGetConnected(EntityUid uid, [NotNullWhen(true)] out IEnumerable<Node>? nodes)
    {
        nodes = null;
        if (TryComp<NodeContainerComponent>(uid, out var cont) && cont.Nodes.TryGetValue("christmaslight", out var node) && node.NodeGroup is not null)
        {
            nodes = node.NodeGroup.Nodes;
            return true;
        }
        return false;
    }
}
