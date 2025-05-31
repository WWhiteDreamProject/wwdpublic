using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.RemoteControl;

public abstract class SharedRemoteControlSystem : EntitySystem
{
    public virtual void RemoteControl(EntityUid user, EntityUid targetEntity, EntityUid? interfaceEntity = null, bool force = false, RemoteControllableComponent? comp = null) { }
    public virtual void EndRemoteControl(EntityUid user) { }
}

public sealed partial class RemoteControlExitActionEvent : InstantActionEvent;
public sealed partial class RemoteControlConsoleSwitchNextActionEvent : InstantActionEvent;

