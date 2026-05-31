using Content.Server._White.Preferences.Managers;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared._White.Humanoid.Systems;
using Content.Shared._White.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        var profile = args.Session != null
            ? _prefs.GetPreferences(args.Session.UserId).SelectedCharacter
            : HumanoidCharacterProfile.Random();


        if (profile?.Species is not { } speciesId || !_proto.TryIndex(speciesId, out var species))
            species = _proto.Index<SpeciesPrototype>(HumanoidProfileSystem.DefaultSpecies);

        if (ent.Comp.SpeciesOverride != null
            && (ent.Comp.AlwaysUseSpeciesOverride
                || (ent.Comp.SpeciesOverrideBlacklist?.Contains(new ProtoId<SpeciesPrototype>(species.ID)) ?? false)))
            species = _proto.Index(ent.Comp.SpeciesOverride.Value);

        args.Entity = Spawn(species.Prototype);
        _humanoid.ApplyProfile(args.Entity.Value, profile!.WithSpecies(species.ID));
    }
}
