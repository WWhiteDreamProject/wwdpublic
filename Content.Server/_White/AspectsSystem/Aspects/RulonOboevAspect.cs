using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server.Access.Systems;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Server.PDA;
using Content.Server.StationRecords.Systems;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class RulonOboevAspect : AspectSystem<RenameCrewAspectComponent>
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

    EntityUid? existing = null;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RenameCrewAspectComponent, ComponentInit>(OnCompInit);

    }

    public override void Shutdown()
    {
        base.Shutdown();
        existing = null;
    }

    private void OnCompInit(EntityUid uid, RenameCrewAspectComponent comp, ComponentInit args)
    {
        if (existing.HasValue) {
            //Sawmill.Error("RulonOboev aspect should only exist as a single gamerule. Stop spamming shit.");
            return; // fuck off we're full
        }
        existing = uid;
        for (int i = 0; i < comp.FirstNames.Count; i++)
        {
            for (int j = 0; j < comp.LastNames.Count; j++)
            {
                comp.RegularNameCombos.Add((i, j));
            }
        }
    }

    private bool PickName(RenameCrewAspectComponent comp, [NotNullWhen(true)] out string? name)
    {
        if (_rng.Prob(comp.SpecialNameChance) && comp.SpecialNames.Count > 0)
        {
            name = _rng.PickAndTake(comp.SpecialNames);
            return true;
        }
        if (comp.RegularNameCombos.Count > 0) {
            (int first, int last) = _rng.PickAndTake(comp.RegularNameCombos);
            name = string.Join(' ', comp.FirstNames[first], comp.LastNames[last]);
            return true;
        }
        name = null;
        return false;
    }

    /// <summary>
    /// copypasted from RenameCommand
    /// </summary>
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!existing.HasValue)
            return;

        var comp = Comp<RenameCrewAspectComponent>(existing.Value);

        EntityUid entityUid = args.Mob;

        if (!PickName(comp, out var name))
            return;

        // Metadata
        var metadata = _entManager.GetComponent<MetaDataComponent>(entityUid);
        var oldName = metadata.EntityName;
        _entManager.System<MetaDataSystem>().SetEntityName(entityUid, name, metadata);

        var minds = _entManager.System<SharedMindSystem>();

        if (minds.TryGetMind(entityUid, out var mindId, out var mind))
        {
            // Mind
            mind.CharacterName = name;
            _entManager.Dirty(mindId, mind);
        }

        // Id Cards
        if (_entManager.TrySystem<IdCardSystem>(out var idCardSystem))
        {
            if (idCardSystem.TryFindIdCard(entityUid, out var idCard))
            {
                idCardSystem.TryChangeFullName(idCard, name, idCard);

                // Records
                // This is done here because ID cards are linked to station records
                if (_entManager.TrySystem<StationRecordsSystem>(out var recordsSystem)
                    && _entManager.TryGetComponent(idCard, out StationRecordKeyStorageComponent? keyStorage)
                    && keyStorage.Key is { } key)
                {
                    if (recordsSystem.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
                    {
                        generalRecord.Name = name;
                    }

                    recordsSystem.Synchronize(key);
                }
            }
        }

        // PDAs
        if (_entManager.TrySystem<PdaSystem>(out var pdaSystem))
        {
            var query = _entManager.EntityQueryEnumerator<PdaComponent>();
            while (query.MoveNext(out var uid, out var pda))
            {
                if (pda.OwnerName == oldName)
                {
                    pdaSystem.SetOwner(uid, pda, name);
                }
            }
        }

        // Admin Overlay
        if (_entManager.TrySystem<AdminSystem>(out var adminSystem)
            && _entManager.TryGetComponent<ActorComponent>(entityUid, out var actorComp))
        {
            adminSystem.UpdatePlayerList(actorComp.PlayerSession);
        }
    }
}

