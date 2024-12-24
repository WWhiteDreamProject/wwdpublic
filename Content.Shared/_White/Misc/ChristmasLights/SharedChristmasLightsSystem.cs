using Content.Shared.Examine;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Misc.ChristmasLights;

public abstract class SharedChristmasLightsSystem : EntitySystem
{
    [Dependency] protected readonly ILocalizationManager _loc = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChristmasLightsComponent, ExaminedEvent>(OnChristmasLightsExamine);

    }

    private void OnChristmasLightsExamine(EntityUid uid, ChristmasLightsComponent comp, ExaminedEvent args) // todo why am i forced to keep this in shared?
    {
        args.PushMarkup(_loc.GetString("christmas-lights-examine-toggle-mode-tip"), 1);
        args.PushMarkup(_loc.GetString("christmas-lights-examine-toggle-brightness-tip"), 0);
    }
}
