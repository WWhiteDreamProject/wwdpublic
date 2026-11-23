using Content.Server.Chat.Managers;
using Content.Shared._White.Pain.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Server._White.Pain.Systems;

public sealed partial class PainfulSystem : SharedPainfulSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeStatus();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdatePainEffects();
    }
}
