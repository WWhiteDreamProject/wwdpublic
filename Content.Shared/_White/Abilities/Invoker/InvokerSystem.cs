using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._White.Abilities.Invoker;

public abstract class SharedInvokerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void AddOrb(EntityUid uid, OrbType newOrb, InvokerComponent component)
    {
        if (component.CurrentOrbs.Count >= 3)
        {
            component.CurrentOrbs.RemoveAt(0);

            if (component.OrbEntities.Count > 0)
            {
                var oldOrb = component.OrbEntities[0];
                component.OrbEntities.RemoveAt(0);
                QueueDel(oldOrb);
            }
        }

        component.CurrentOrbs.Add(newOrb);

        string prototype = newOrb switch
        {
            OrbType.Quas => "InvokerOrbQuas",
            OrbType.Wex => "InvokerOrbWex",
            OrbType.Exort => "InvokerOrbExort",
            _ => "InvokerOrbQuas"
        };

        var xform = Transform(uid);
        var orbEnt = SpawnAttachedTo(prototype, xform.Coordinates);
        _xform.SetParent(orbEnt, uid);

        component.OrbEntities.Add(orbEnt);

        RearrangeOrbs(component);
    }

    private void RearrangeOrbs(InvokerComponent component)
    {
        for (int i = 0; i < component.OrbEntities.Count; i++)
        {
            var orb = component.OrbEntities[i];
            if (!Exists(orb))
                continue;

            var orbXform = Transform(orb);

            if (i < component.OrbOffsets.Count)
            {
                _xform.SetLocalPosition(orbXform.Owner, component.OrbOffsets[i]);
            }
        }
    }

    public void Invoke(EntityUid uid, InvokerComponent component)
    {
        if (component.CurrentOrbs.Count < 3)
            return;

        if (!_protoMan.TryIndex<InvokerSpellPoolPrototype>(component.SpellPool, out var pool))
            return;

        InvokerSpellPrototype? matchedSpell = null;

        foreach (var spellId in pool.Spells)
        {
            if (!_protoMan.TryIndex<InvokerSpellPrototype>(spellId, out var spell))
                continue;

            if (CompareCombinations(component.CurrentOrbs, spell.Combination))
            {
                matchedSpell = spell;
                break;
            }
        }

        if (matchedSpell == null)
            return;

        var actionProtoId = matchedSpell.Action.ToString();
        foreach (var activeAction in component.ActiveSpellActions)
        {
            if (EntityManager.TryGetComponent<MetaDataComponent>(activeAction, out var meta) &&
                meta.EntityPrototype != null &&
                meta.EntityPrototype.ID == actionProtoId)
            {
                return;
            }
        }

        if (component.ActiveSpellActions.Count >= component.MaxActiveSpells)
        {
            var oldAction = component.ActiveSpellActions[0];
            component.ActiveSpellActions.RemoveAt(0);

            SaveCooldown(oldAction, component);
            _actions.RemoveAction(uid, oldAction);
        }

        var newAction = _actions.AddAction(uid, matchedSpell.Action);
        if (newAction != null)
        {
            component.ActiveSpellActions.Add(newAction.Value);

            RestoreCooldown(newAction.Value, component, actionProtoId);
        }

        UpdateSpellComponents(uid, component, matchedSpell);
    }

    private void UpdateSpellComponents(EntityUid uid, InvokerComponent component, InvokerSpellPrototype matchedSpell)
    {
        if (component.LastSpellComponents != null)
        {
            foreach (var compName in component.LastSpellComponents)
            {
                var compType = _factory.GetRegistration(compName).Type;
                _entMan.RemoveComponentDeferred(uid, compType);
            }
        }

        component.LastSpellComponents = new();

        if (matchedSpell.Components != null)
        {
            foreach (var (compName, data) in matchedSpell.Components)
            {
                var compRegistration = _factory.GetRegistration(compName);
                var newComp = (Component) _factory.GetComponent(compRegistration);

                _entMan.AddComponent(uid, newComp);
                component.LastSpellComponents.Add(compName);
            }
        }
    }

    private bool CompareCombinations(List<OrbType> current, List<OrbType> recipe)
    {
        if (current.Count != recipe.Count) return false;

        var curSorted = current.OrderBy(x => x).ToList();
        var recSorted = recipe.OrderBy(x => x).ToList();

        for (int i = 0; i < curSorted.Count; i++)
        {
            if (curSorted[i] != recSorted[i]) return false;
        }
        return true;
    }

    public void SaveCooldown(EntityUid? actionUid, InvokerComponent comp)
    {
        if (!_actions.TryGetActionData(actionUid, out var actionComponent))
            return;

        if (!actionComponent.Cooldown.HasValue)
            return;

        var cooldown = actionComponent.Cooldown.Value;
        var remaining = cooldown.End - _timing.CurTime;

        if (remaining > TimeSpan.Zero)
        {
            if (!TryComp<MetaDataComponent>(actionUid, out var meta) || meta.EntityPrototype == null)
                return;

            var protoId = meta.EntityPrototype.ID;
            comp.CooldownHistory[protoId] = remaining;
        }
        else
        {
            if (TryComp<MetaDataComponent>(actionUid, out var meta) && meta.EntityPrototype != null)
            {
                comp.CooldownHistory.Remove(meta.EntityPrototype.ID);
            }
        }
    }

    public void RestoreCooldown(EntityUid actionUid, InvokerComponent comp, string actionProtoId)
    {
        if (!_actions.TryGetActionData(actionUid, out var actionComponent))
            return;

        if (!comp.CooldownHistory.TryGetValue(actionProtoId, out var remainingTime))
            return;

        if (remainingTime > TimeSpan.Zero)
        {
            var start = _timing.CurTime;
            var end = start + remainingTime;
            _actions.SetCooldown(actionUid, start, end);
        }
        else
        {
            comp.CooldownHistory.Remove(actionProtoId);
        }
    }
}
