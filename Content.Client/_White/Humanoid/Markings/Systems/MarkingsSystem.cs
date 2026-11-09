using Content.Client.DisplacementMap;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Systems;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Client._White.Humanoid.Markings.Systems;

public sealed partial class MarkingsSystem : SharedMarkingsSystem
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    private bool _clientCensorNudity;
    private bool _serverCensorNudity;

    public bool CensorNudity => _clientCensorNudity || _serverCensorNudity;

    public override void Initialize()
    {
        base.Initialize();

        InitializeProvider();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

        Subs.CVar(_configuration, CCVars.AccessibilityClientCensorNudity,  SetClientCensorNudity, true);
        Subs.CVar(_configuration, CCVars.AccessibilityServerCensorNudity, SetServerCensorNudity, true);
    }

    #region Event Handling

    private void SetClientCensorNudity(bool value)
    {
        _clientCensorNudity = value;
        UpdateMarkings();
    }

    private void SetServerCensorNudity(bool value)
    {
        _serverCensorNudity = value;
        UpdateMarkings();
    }

    #endregion

    #region Private API

    private void UpdateMarkings()
    {
        var query = EntityQueryEnumerator<MarkingsProviderComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateMarkings((uid, comp));
        }
    }

    #endregion
}
