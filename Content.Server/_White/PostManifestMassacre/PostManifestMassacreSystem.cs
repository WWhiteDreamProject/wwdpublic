using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Humanoid;
using Content.Shared._White;
using Content.Shared.EntityList;
using Content.Shared.Hands.EntitySystems;
using Content.Server.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.PostManifestMassacre;
public sealed class PostManifestMassacreSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    [ValidatePrototypeId<EntityListPrototype>]
    private const string weaponsPrototypeId = "PostManifestMassacreWeapons";
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndedEvent>(onRoundEnded);
    }

    private void onRoundEnded(RoundEndedEvent ev)
    {
        if(!_cfg.GetCVar(WhiteCVars.PMMEnabled))
            return;

        if (!_prototypeManager.TryIndex(weaponsPrototypeId, out EntityListPrototype? prototype))
            return;
            
        var weapons = prototype.EntityIds;

        var players = AllEntityQuery<HumanoidAppearanceComponent, ActorComponent, MobStateComponent>();

        while (players.MoveNext(out var uid, out _, out _, out var mob))
        {
            if (!_mobState.IsAlive(uid, mob))
                continue;
            
            var weapon = Spawn(_robustRandom.Pick(weapons), Transform(uid).Coordinates);
            _hands.TryPickup(uid, weapon);
        }
    }
}