// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <ilyukarno@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Goobstation.ItemMiner.Components;

[RegisterComponent]
public sealed partial class TelecrystalMinerComponent : Component
{
    /// <summary>
    /// How many telecrystals have we produced so far.
    /// </summary>
    [DataField]
    public int Accumulated = 0;

    [DataField]
    public TCMinerStage NotifiedStage = TCMinerStage.Initial;

    /// <summary>
    /// After how many telecrystals produced to make an announcement.
    /// </summary>
    [DataField]
    public int AnnounceAt = 40;

    [DataField]
    public LocId Announcement = "telecrystal-miner-announcement";

    /// <summary>
    /// After how many telecrystals produced to make an announcement with the miner's location.
    /// </summary>
    [DataField]
    public int LocationAt = 100;

    [DataField]
    public LocId LocationAnnouncement = "telecrystal-miner-announcement2";
}

public enum TCMinerStage
{
    Initial,
    FirstAnnounced,
    LocationAnnounced
}
