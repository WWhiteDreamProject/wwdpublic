using Content.Shared._White.Actions.Events;
using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Humanoid;

namespace Content.Server._White.Abilities.Psionics.Abilities
{
    public sealed class PolymorphPowerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly MetaDataSystem _meta = default!;
        [Dependency] private readonly SharedHumanoidAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PolymorphPowerActionEvent>(OnPowerUsed);
            SubscribeLocalEvent<PolymorphPowerComponent, PolymorphPowerRevertActionEvent>(OnUsed);
            SubscribeLocalEvent<PolymorphPowerComponent, ComponentStartup>(ComponentStart);
        }

        public void OnPowerUsed(PolymorphPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, args.Target, "polymorph", true))
                return;
            if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var humanoid))
                return;

            var target = args.Target;
            var user = args.Performer;

            if (TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid) &&
                TryComp<HumanoidAppearanceComponent>(user, out var userHumanoid))
            {

                var meta = MetaData(target);
                _meta.SetEntityName(user, meta.EntityName);
                _meta.SetEntityDescription(user, meta.EntityDescription);

                userHumanoid.Species = targetHumanoid.Species;
                userHumanoid.Sex = targetHumanoid.Sex;
                userHumanoid.Age = targetHumanoid.Age;
                userHumanoid.SkinColor = targetHumanoid.SkinColor;
                userHumanoid.CustomBaseLayers = targetHumanoid.CustomBaseLayers;
                userHumanoid.Gender = targetHumanoid.Gender;
                userHumanoid.Width = targetHumanoid.Width;
                userHumanoid.Height = targetHumanoid.Height;
                userHumanoid.MarkingSet = targetHumanoid.MarkingSet;
                userHumanoid.Voice = targetHumanoid.Voice;
                userHumanoid.BodyType = targetHumanoid.BodyType;

                Dirty(user, userHumanoid);
            }

            _psionics.LogPowerUsed(args.Performer, "polymorph");
            args.Handled = true;
        }

        public void OnUsed(EntityUid uid, PolymorphPowerComponent comp, PolymorphPowerRevertActionEvent args)
        {
            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
                return;

            var effect = Spawn("PsionicPolymorphEffect", _transform.GetMapCoordinates(uid));
            _transform.SetParent(effect, uid);

            _meta.SetEntityName(uid, comp.OriginalName);
            _meta.SetEntityDescription(uid, comp.OriginalDescription);
            _appearance.LoadProfile(uid, humanoid.LastProfileLoaded);

            args.Handled = true;
        }

        public void ComponentStart(EntityUid uid, PolymorphPowerComponent component, ComponentStartup args)
        {
            var meta = MetaData(uid);
            component.OriginalName = meta.EntityName;
            component.OriginalDescription = meta.EntityDescription;
        }
    }
}
