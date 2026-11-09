using Content.Server.CharacterAppearance.Components;
using Content.Shared._White.Humanoid.Components;
using Content.Shared._White.Humanoid.Systems;
using Content.Shared._White.Preferences;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Server.Humanoid.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        // If we have an initial profile/base layer set, do not randomize this humanoid.
        if (!TryComp(uid, out HumanoidProfileComponent? humanoid)/* || !string.IsNullOrEmpty(humanoid.Initial)*/)
        {
            return;
        }

        var profile = HumanoidCharacterProfile.Random(humanoid.Species);/*
        //If we have a specified hair style, change it to this
        if(component.Hair != null) TODO
            profile = profile.WithCharacterAppearance(profile.Appearance.WithHairStyleName(component.Hair));*/

        _humanoid.ApplyProfile((uid, humanoid), profile);

        if (component.RandomizeName)
            _metaData.SetEntityName(uid, profile.Name);
    }
}
