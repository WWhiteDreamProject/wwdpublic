using Content.Shared._White.Body.Systems;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;

namespace Content.Client._White.Body.Systems;

public sealed partial class BodySystem : SharedBodySystem
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeAppearance();
    }
}
