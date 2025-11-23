// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Goobstation.Devil;
using Content.Server.Body.Components;
using Content.Shared._White.Body.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Server._Goobstation.Devil.Contract;

public sealed partial class DevilContractSystem
{
    private void InitializeSpecialActions()
    {
        SubscribeLocalEvent<DevilContractSoulOwnershipEvent>(OnSoulOwnership);
        SubscribeLocalEvent<DevilContractLoseHandEvent>(OnLoseHand);
        SubscribeLocalEvent<DevilContractLoseLegEvent>(OnLoseLeg);
        SubscribeLocalEvent<DevilContractLoseOrganEvent>(OnLoseOrgan);
        SubscribeLocalEvent<DevilContractChanceEvent>(OnChance);
    }
    private void OnSoulOwnership(DevilContractSoulOwnershipEvent args)
    {
        if (args.Contract?.ContractOwner is not { } contractOwner)
            return;

        TryTransferSouls(contractOwner, args.Target, 1);
    }

    private void OnLoseHand(DevilContractLoseHandEvent args)
    {
        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;
        //
        // var hands = _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand, body).ToList();
        //
        // if (hands.Count <= 0)
        //     return;
        //
        // var pick = _random.Pick(hands);

        // if (!TryComp<WoundableComponent>(pick.Id, out var woundable)
        //     || !woundable.ParentWoundable.HasValue)
        //     return;
        //
        // _wounds.AmputateWoundableSafely(woundable.ParentWoundable.Value, pick.Id, woundable);
        // QueueDel(pick.Id);
        //
        // Dirty(args.Target, body);
        // _sawmill.Debug($"Removed part {ToPrettyString(pick.Id)} from {ToPrettyString(args.Target)}");
        // QueueDel(pick.Id);

        var baseXform = Transform(args.Target);
        foreach (var part in _bodySystem.GetBodyParts((args.Target, body), BodyPartType.Hand)) // WD EDIT
        {
            _transform.AttachToGridOrMap(part.Owner); // WD EDIT
            break;
        }
    }

    private void OnLoseLeg(DevilContractLoseLegEvent args)
    {
        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;

        // var legs = _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Leg, body).ToList();
        //
        // if (legs.Count <= 0)
        //     return;
        //
        // var pick = _random.Pick(legs);
        //
        // if (!TryComp<WoundableComponent>(pick.Id, out var woundable)
        //     || !woundable.ParentWoundable.HasValue)
        //     return;
        //
        // _wounds.AmputateWoundableSafely(woundable.ParentWoundable.Value, pick.Id, woundable);
        //
        // Dirty(args.Target, body);
        // _sawmill.Debug($"Removed part {ToPrettyString(pick.Id)} from {ToPrettyString(args.Target)}");
        // QueueDel(pick.Id);
        var baseXform = Transform(args.Target);
        foreach (var part in _bodySystem.GetBodyParts((args.Target, body), BodyPartType.Leg)) // WD EDIT
        {
            _transform.AttachToGridOrMap(part.Owner);
            break;
        }
    }

    private void OnLoseOrgan(DevilContractLoseOrganEvent args)
    {
        // WD EDIT START
        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;

        // don't remove the brain, as funny as that is.
        var eligibleOrgans = _bodySystem.GetOrgans((args.Target, body))
            .Where(o => !HasComp<BrainComponent>(o.Owner))
            .ToList();
        // WD EDIT END

        if (eligibleOrgans.Count <= 0)
            return;

        var pick = _random.Pick(eligibleOrgans);

        QueueDel(pick.Owner); // WD EDIT
    }

    // LETS GO GAMBLING!!!!!
    private void OnChance(DevilContractChanceEvent args)
    {
        AddRandomClause(args.Target);
    }
}
