// SPDX-FileCopyrightText: 2025 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Lighting.Shaders;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class LightingOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private LightingOverlay? _lightingOverlay;

    public override void Initialize()
    {
        base.Initialize();

        _lightingOverlay = new LightingOverlay(EntityManager, _prototypeManager);
        _overlayManager.AddOverlay(_lightingOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_lightingOverlay != null)
        {
            _overlayManager.RemoveOverlay(_lightingOverlay);
            _lightingOverlay.Dispose();
            _lightingOverlay = null;
        }
    }
}
