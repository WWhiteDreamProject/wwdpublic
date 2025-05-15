using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RegulatorLampComponent : Component
{
    /// <summary>
    /// Temperature below which the lamp is guaranteed to work
    /// Increased by 273.15f upon component init.
    /// </summary>
    [DataField("safeTemp", required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SafeTemperature = 450;

    /// <summary>
    /// Temperature at or above which the lamp is guaranteed to break immediately after shooting
    /// Increased by 273.15f upon component init.
    /// </summary>
    [DataField("unsafeTemp", required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float UnsafeTemperature = 750;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Intact = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BreakSound = new SoundCollectionSpecifier("GlassBreak");
}
