using Content.Shared._White.BloodCult.Runes.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Whitelist;

namespace Content.Shared._White.BloodCult.Runes;

public abstract class SharedBloodCultRuneSystem : EntitySystem
{
    [Dependency] protected readonly EntityWhitelistSystem EntityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultRuneComponent, ExamineAttemptEvent>(OnRuneExamineAttempt);
    }

    private void OnRuneExamineAttempt(Entity<BloodCultRuneComponent> ent, ref ExamineAttemptEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner) && EntityWhitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Examiner))
            args.Cancel();
    }
}
