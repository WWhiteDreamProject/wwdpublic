using Content.Server.Mood;
using Content.Shared._Shitmed.Medical.Surgery;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;

namespace Content.Server._White.Mood;

/// <summary>
/// Updates the mood when the effect of a painkiller (Morphine) starts/ends
/// Everything else is handled by MoodSystem.OnDamageChange
/// </summary>
public sealed class PainkillerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MoodSystem _mood = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoScreamComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<NoScreamComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(EntityUid uid, NoScreamComponent component, ComponentStartup args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled) || !TryComp<MoodComponent>(uid, out var mood))
            return;

        _mood.UpdateDamageState(uid, mood);
    }

    private void OnRemove(EntityUid uid, NoScreamComponent component, ComponentRemove args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled) || !TryComp<MoodComponent>(uid, out var mood))
            return;

        _mood.UpdateDamageState(uid, mood);
    }

}
