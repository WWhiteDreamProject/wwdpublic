using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Misc.ChristmasLights;

[Serializable, NetSerializable]
public sealed class ChangeChristmasLightsModeAttemptEvent : EntityEventArgs
{
    public NetEntity target;

    public ChangeChristmasLightsModeAttemptEvent(NetEntity target) { this.target = target; }
}

[Serializable, NetSerializable]
public sealed class ChangeChristmasLightsBrightnessAttemptEvent : EntityEventArgs
{
    public NetEntity target;

    public ChangeChristmasLightsBrightnessAttemptEvent(NetEntity target) { this.target = target; }
}

