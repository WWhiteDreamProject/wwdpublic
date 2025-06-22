using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns;

[RegisterComponent]
public sealed partial class ProjectileDamageModifierComponent : Component
{
    [DataField(required: true)]
    public List<WhitelistMultiplierPair> Multipliers = new();

    [DataField]
    public float DefaultMultiplier = 1f;

}

[DataDefinition]
public sealed partial class WhitelistMultiplierPair
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField(required: true)]
    public float Multiplier;
}

public sealed class ProjectileDamageMultiplierSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileDamageModifierComponent, ProjectileHitEvent>(OnHit);
    }

    private void OnHit(EntityUid uid, ProjectileDamageModifierComponent comp, ref ProjectileHitEvent args)
    {
        foreach(var pair in comp.Multipliers)
        {
            if (_whitelist.IsWhitelistPass(pair.Whitelist, args.Target))
            {
                args.Damage *= pair.Multiplier;
                return;
            }
        }
        args.Damage *= comp.DefaultMultiplier;
    }
}
