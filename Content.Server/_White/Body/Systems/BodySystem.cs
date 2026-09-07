using Content.Shared._White.Body.Systems;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem : SharedBodySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeRelay();
    }
}
