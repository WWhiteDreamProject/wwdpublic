// SPDX-FileCopyrightText: 2025 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> EnableLightsGlowing =
        CVarDef.Create("light.enable_lights_glowing", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
