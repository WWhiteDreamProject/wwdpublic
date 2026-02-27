using Content.Shared._White.Pain.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._White.Pain.Systems;

public sealed partial class PainfulSystem : SharedPainfulSystem
{
    [Dependency] private readonly SpriteSystem _system = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeStatus();
    }
}
