using Content.Shared._White;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.AntiParkinsons;

// The following code is slightly esoteric and higly schizophrenic. You have been warned.

#pragma warning disable RA0002

public sealed class AntiParkinsonsRevertSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _refl = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;


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
        var query = AllEntityQuery<PixelSnapEyeComponent>();

        while (query.MoveNext(out var uid, out var ppComp))
        {
            if (!TryComp<EyeComponent>(uid, out var eyeComp) || eyeComp.Eye == null)
                continue;

            eyeComp.Eye.Position = PPCamHelper.CheckForChange(eyeComp.Eye.Position, ppComp.EyePositionModified, ppComp.EyePosition);
            eyeComp.Eye.Offset = PPCamHelper.CheckForChange(eyeComp.Eye.Offset, ppComp.EyeOffsetModified, ppComp.EyeOffset);
            eyeComp.Offset = eyeComp.Eye.Offset;

            if(TryComp<SpriteComponent>(uid, out var sprite))
                sprite.Offset = PPCamHelper.CheckForChange(sprite.Offset, ppComp.SpriteOffsetModified, ppComp.SpriteOffset);
        }
    }
}

#pragma warning restore RA0002
