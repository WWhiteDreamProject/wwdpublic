using System.Collections.Generic;
using Content.Shared._NC.Forensics;

namespace Content.Server._NC.Forensics;

[RegisterComponent]
public sealed partial class NcpdForensicsStationComponent : Component
{
    public List<ForensicsAlertData> Alerts = new();
}
