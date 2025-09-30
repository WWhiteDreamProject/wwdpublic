using System.Linq;
using System.Numerics;
using Content.Server.Popups;
using Content.Shared._White.BloodCult;
using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared._White.BloodCult.Components;
using Content.Shared._White.BloodCult.Construct;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._White.BloodCult.Pylon;

public sealed class PylonSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DamageOnInteractSystem _damageOnInteract = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turfs = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PylonComponent, InteractHandEvent>(OnInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var pylonQuery = EntityQueryEnumerator<PylonComponent>();
        while (pylonQuery.MoveNext(out var uid, out var pylon))
        {
            if (!pylon.IsActive)
                continue;

            pylon.CorruptionAccumulator += frameTime;
            pylon.HealingAccumulator += frameTime;

            if (pylon.CorruptionAccumulator >= pylon.CorruptionCooldown)
            {
                pylon.CorruptionAccumulator = 0;
                CorruptTilesInRange((uid, pylon));
            }

            if (pylon.HealingAccumulator >= pylon.HealingCooldown)
            {
                pylon.HealingAccumulator = 0;
                HealInRange((uid, pylon));
            }
        }
    }

    private void OnInteract(Entity<PylonComponent> pylon, ref InteractHandEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.User))
            return;

        ToggleActive(pylon, !pylon.Comp.IsActive);
        var toggleMsg = Loc.GetString(pylon.Comp.IsActive ? "pylon-toggle-on" : "pylon-toggle-off");
        _popup.PopupEntity(toggleMsg, pylon);
    }

    private void ToggleActive(Entity<PylonComponent> pylon, bool state)
    {
        pylon.Comp.IsActive = state;

        _appearance.SetData(pylon, PylonVisuals.Activated, state);
        _pointLight.SetEnabled(pylon, state);

        if (TryComp<DamageOnInteractComponent>(pylon, out var damageOnInteractComp))
            _damageOnInteract.SetIsDamageActiveTo((pylon, damageOnInteractComp), state);
    }

    private void CorruptTilesInRange(Entity<PylonComponent> pylon)
    {
        var pylonTrans = Transform(pylon);
        if (pylonTrans.GridUid is not { } gridUid || !TryComp(pylonTrans.GridUid, out MapGridComponent? mapGrid))
            return;

        var radius = pylon.Comp.CorruptionRadius;
        var tilesRefs = _map.GetLocalTilesIntersecting(gridUid,
                mapGrid,
                new Box2(pylonTrans.Coordinates.Position + new Vector2(-radius, -radius),
                    pylonTrans.Coordinates.Position + new Vector2(radius, radius)))
            .ToList();

        _random.Shuffle(tilesRefs);

        var cultTileDefinition = (ContentTileDefinition) _tileDefinition[pylon.Comp.CultTile];
        foreach (var tile in tilesRefs)
        {
            if (tile.Tile.TypeId == cultTileDefinition.TileId)
                continue;

            var tilePos = _turfs.GetTileCenter(tile);
            _audio.PlayPvs(pylon.Comp.CorruptTileSound, tilePos, AudioParams.Default.WithVolume(-5));
            _tile.ReplaceTile(tile, cultTileDefinition);
            Spawn(pylon.Comp.TileCorruptEffect, tilePos);
            return;
        }
    }

    private void HealInRange(Entity<PylonComponent> pylon)
    {
        var pylonPosition = Transform(pylon).Coordinates;
        var targets =
            _lookup.GetEntitiesInRange<MobStateComponent>(pylonPosition, pylon.Comp.HealingAuraRange);

        foreach (var target in targets)
        {
            if (HasComp<BloodCultistComponent>(target) && !_mobState.IsDead(target))
                _damageable.TryChangeDamage(target, pylon.Comp.Healing, true);
        }
    }
}
