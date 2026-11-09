using Content.Server.Chat.Managers;
using Content.Shared._White.Pain.Systems;
using Robust.Server.Player;

namespace Content.Server._White.Pain.Systems;

public sealed partial class PainfulSystem : SharedPainfulSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeStatus();
    }
}
