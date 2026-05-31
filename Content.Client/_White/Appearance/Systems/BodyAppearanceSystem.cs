using Content.Shared._White.Appearance.Systems;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;

namespace Content.Client._White.Appearance.Systems;

public sealed partial class BodyAppearanceSystem : SharedBodyAppearanceSystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeProvider();
    }
}
