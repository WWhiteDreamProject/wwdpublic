using Robust.Server.GameStates;

namespace Content.Server._White.PVS;

public sealed class PVSIgnoreSystem : EntitySystem
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PVSIgnoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PVSIgnoreComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, PVSIgnoreComponent comp, ComponentStartup args) => _pvsOverride.AddGlobalOverride(uid);

    private void OnShutdown(EntityUid uid, PVSIgnoreComponent comp, ComponentShutdown args) => _pvsOverride.RemoveGlobalOverride(uid);
}
