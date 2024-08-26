using Content.Client.Hands.Systems;
using Content.Client.NPC.HTN;
using Content.Shared._White.Intent;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client._White.Intent;

public sealed class IntentSystem : SharedIntentSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public event Action<bool>? LocalPlayerIntentUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntentComponent, AfterAutoHandleStateEvent>(OnHandleState);

        Subs.CVar(_cfg, CCVars.CombatModeIndicatorsPointShow, OnShowIntentIndicatorsChanged, true);
    }

    private void OnHandleState(EntityUid uid, IntentComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateHud(uid);
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<IntentIndicatorsOverlay>();

        base.Shutdown();
    }

    public override void SetIntent(EntityUid entity, Shared._White.Intent.Intent intent = Shared._White.Intent.Intent.Help, IntentComponent? component = null)
    {
        base.SetIntent(entity, intent, component);
        UpdateHud(entity);
    }

    protected override bool IsNpc(EntityUid uid)
    {
        return HasComp<HTNComponent>(uid);
    }

    private void UpdateHud(EntityUid entity)
    {
        if (entity != _playerManager.LocalEntity || !_timing.IsFirstTimePredicted)
            return;

        var inCombatMode = CanAttack();
        LocalPlayerIntentUpdated?.Invoke(inCombatMode);
    }

    private void OnShowIntentIndicatorsChanged(bool isShow)
    {
        if (isShow)
        {
            _overlayManager.AddOverlay(new IntentIndicatorsOverlay(
                _inputManager,
                EntityManager,
                _eye,
                this,
                EntityManager.System<HandsSystem>()));
        }
        else
            _overlayManager.RemoveOverlay<IntentIndicatorsOverlay>();
    }

    public bool CanAttack()
    {
        return CanAttack(_playerManager.LocalEntity);
    }

    public Shared._White.Intent.Intent? GetIntent()
    {
        return GetIntent(_playerManager.LocalEntity);
    }
}
