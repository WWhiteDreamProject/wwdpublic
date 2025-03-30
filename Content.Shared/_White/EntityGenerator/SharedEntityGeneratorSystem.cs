using Content.Shared.Verbs;

namespace Content.Shared._White.EntityGenerator;


public abstract class SharedEntityGeneratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityGeneratorComponent, GetVerbsEvent<AlternativeVerb>>(AddExtractVerb);
    }

    private void AddExtractVerb(EntityUid uid, EntityGeneratorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => Extract(uid, args.User, component),
            Text = Loc.GetString("entity-generator-extract-verb"),
            Disabled = component.Charges < 0
        });
    }

    protected abstract void Extract(EntityUid uid, EntityUid user, EntityGeneratorComponent component);
}
