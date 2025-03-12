using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Hands;

[Serializable, NetSerializable]
public sealed class HandDeselectedNetworkCrutchWrap(NetEntity target, NetEntity user) : EntityEventArgs
{
    public NetEntity Target = target;
    public NetEntity User = user;
}
