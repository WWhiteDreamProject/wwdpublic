using System.Collections;
using System.Linq;
using Content.Shared._White.Damage.Prototypes;
using Content.Shared._White.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Damage;

/// <summary>
/// This class represents a collection of damage types and damage values.
/// </summary>
/// <remarks>
/// The actual damage information is stored in <see cref="Damage"/>. This class provides
/// functions to apply resistance sets and supports basic math operations to modify this dictionary.
/// </remarks>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class DamageSpecifier : IDictionary<ProtoId<DamageTypePrototype>, FixedPoint2>, IEquatable<DamageSpecifier>
{
    /// <summary>
    /// Damage dictionary. Most functions exist to somehow modifying this.
    /// </summary>
    [DataField]
    private Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> Damage { get; set; } = new();

    /// <summary>
    /// Whether this damage specifier has any entries.
    /// </summary>
    public bool Empty => Damage.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the <see cref="Damage"/> is read-only.
    /// </summary>
    /// <returns>True if the <see cref="Damage"/> is read-only; otherwise, false.</returns>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets a collection containing the damage.
    /// </summary>
    /// <returns>A <see cref="ICollection{FixedPoint2}"/> containing damage in the <see cref="Damage"/>.</returns>
    public ICollection<FixedPoint2> Values => Damage.Values;

    /// <summary>
    /// Gets a collection containing the damage types.
    /// </summary>
    /// <returns>A <see cref="ICollection{ProtoId}"/> containing the types of the damage in the <see cref="Damage"/>.</returns>
    public ICollection<ProtoId<DamageTypePrototype>> Keys => Damage.Keys;

    /// <summary>
    /// Gets a value indicating whether the <see cref="Damage"/> is read-only.
    /// </summary>
    /// <returns>True if the <see cref="Damage"/> is read-only; otherwise, false.</returns>
    public FixedPoint2 this[ProtoId<DamageTypePrototype> key]
    {
        get => Damage[key];
        set => Damage[key] = value;
    }

    /// <summary>
    /// Gets the number of key/value pairs contained in the <see cref="Damage"/>.
    /// </summary>
    /// <returns>The number of key/value pairs contained in the <see cref="Damage"/>.</returns>
    public int Count => Damage.Count;

    /// <summary>
    /// Constructor that just results in an empty dictionary.
    /// </summary>
    public DamageSpecifier() { }

    /// <summary>
    /// Constructor that takes another DamageSpecifier instance and copies it.
    /// </summary>
    public DamageSpecifier(DamageSpecifier damageSpec)
    {
        Damage = new(damageSpec.Damage);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="Damage"/>.
    /// </summary>
    /// <returns>An Enumerator structure for the <see cref="Damage"/>.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Constructor that takes a single damage group prototype and a damage value.
    /// The value is divided between members of the damage group.
    /// </summary>
    public DamageSpecifier(ProtoId<DamageGroupPrototype> group, FixedPoint2 value, DamageableSystem system)
    {
        var types = system.GetTypes(group);

        var remainingTypes = types.Count;
        var remainingDamage = value;

        foreach (var type in types)
        {
            var damage = remainingDamage / FixedPoint2.New(remainingTypes);
            Damage.Add(type, damage);
            remainingDamage -= damage;
            remainingTypes -= 1;
        }
    }

    /// <summary>
    /// Constructor that takes a single damage type and a damage value.
    /// </summary>
    public DamageSpecifier(ProtoId<DamageTypePrototype> type, FixedPoint2 value)
    {
        Damage = new() { { type, value } };
    }

    /// <summary>
    /// Indicates whether the current specifier contains any positive damage.
    /// </summary>
    /// <returns>True if the specifier contains any positive damage; otherwise, false.</returns>
    public bool AnyPositive()
    {
        foreach (var value in Damage.Values)
        {
            if (value <= FixedPoint2.Zero)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="Damage"/> contains a specific value.
    /// </summary>
    /// <param name="keyValue">The key and value to locate in the <see cref="Damage"/>.</param>
    /// <returns>True if item is found in the <see cref="Damage"/>; otherwise, false</returns>
    public bool Contains(KeyValuePair<ProtoId<DamageTypePrototype>, FixedPoint2> keyValue)
    {
        return Damage.Contains(keyValue);
    }

    /// <summary>
    /// Determines whether the <see cref="Damage"/> contains an element with the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="Damage"/>.</param>
    /// <returns>True if the <see cref="Damage"/> contains an element with the key; otherwise, false.</returns>
    public bool ContainsKey(ProtoId<DamageTypePrototype> key)
    {
        return Damage.ContainsKey(key);
    }

    /// <summary>
    /// Determines whether the <see cref="Damage"/> contains a specific value.
    /// </summary>
    /// <param name="value">The value to locate in the <see cref="Damage"/>.</param>
    /// <returns>true if the <see cref="Damage"/>contains an element with the specified value; otherwise, false.</returns>
    public bool ContainsValue(FixedPoint2 value)
    {
        return Damage.ContainsValue(value);
    }

    /// <summary>
    /// Indicates whether the current specifier is equal to another specifier.
    /// </summary>
    /// <param name="other">A specifier to compare with this object.</param>
    /// <returns>True if the current specifier is equal to the other; otherwise, false.</returns>
    public bool Equals(DamageSpecifier? other)
    {
        if (other == null || Damage.Count != other.Damage.Count)
            return false;

        foreach (var (type, damage) in Damage)
        {
            if (!other.Damage.TryGetValue(type, out var otherDamage))
                return false;

            if (damage == otherDamage)
                continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes the damage with the specified type from the <see cref="Damage"/>.
    /// </summary>
    /// <param name="keyValue">The type of the damage to remove.</param>
    /// <returns>True if the damage is successfully removed; otherwise, false.</returns>
    public bool Remove(KeyValuePair<ProtoId<DamageTypePrototype>, FixedPoint2> keyValue)
    {
        return Remove(keyValue.Key);
    }

    /// <summary>
    /// Removes the damage with the specified type from the <see cref="Damage"/>.
    /// </summary>
    /// <param name="key">The type of the damage to remove.</param>
    /// <returns>True if the damage is successfully removed; otherwise, false.</returns>
    public bool Remove(ProtoId<DamageTypePrototype> key)
    {
        return Damage.Remove(key);
    }

    /// <summary>
    /// Add up all the damage values for damage types that are members of a given group.
    /// </summary>
    /// <returns>True if members of the group are included in this specifier, false otherwise.</returns>
    public bool TryGetDamageInGroup(ProtoId<DamageGroupPrototype> group, DamageableSystem system, out FixedPoint2 total)
    {
        var types = system.GetTypes(group);

        var contains = false;
        total = FixedPoint2.Zero;

        foreach (var type in types)
        {
            if (!Damage.TryGetValue(type, out var value))
                continue;

            total += value;
            contains = true;
        }

        return contains;
    }

    /// <summary>
    /// Gets the damage associated with the specified type.
    /// </summary>
    /// <param name="key">The type of the damage to get.</param>
    /// <param name="value">When this method returns, contains the damage associated with the specified type, if the key is found; otherwise, null.</param>
    /// <returns>True if the <see cref="DamageSpecifier"/> contains damage with the specified type; otherwise, false.</returns>
    public bool TryGetValue(ProtoId<DamageTypePrototype> key, out FixedPoint2 value)
    {
        return Damage.TryGetValue(key, out value);
    }

    /// <summary>
    /// Returns a new instance of <see cref="DamageSpecifier"/> with the same values as this instance.
    /// </summary>
    /// <returns>A new instance of <see cref="DamageSpecifier"/> with the same values as this instance.</returns>
    public DamageSpecifier Clone()
    {
        return new (this);
    }

    /// <summary>
    /// Returns a dictionary using <see cref="DamageGroupPrototype.ID"/> keys, with values calculated by adding
    /// up the values for each damage type in that group
    /// </summary>
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> GetDamagePerGroup(DamageableSystem system, IPrototypeManager manager)
    {
        var dict = new Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2>();

        foreach (var group in manager.EnumeratePrototypes<DamageGroupPrototype>())
        {
            if (!TryGetDamageInGroup(group, system, out var value))
                continue;

            dict.Add(group.ID, value);
        }

        return dict;
    }

    /// <summary>
    /// Returns a sum of the damage values.
    /// </summary>
    /// <remarks>
    /// Note that this being zero does not mean this damage has no effect.
    /// Healing in one type may cancel damage in another.
    /// Consider using <see cref="AnyPositive"/> or <see cref="Empty"/> instead.
    /// </remarks>
    public FixedPoint2 GetTotal()
    {
        var total = FixedPoint2.Zero;

        foreach (var value in Damage.Values)
        {
            total += value;
        }

        return total;
    }

    /// <summary>
    /// Tries to get the damage associated with the specified type in the <see cref="Damage"/>.
    /// </summary>
    /// <returns>When the method is successful, the returned damage associated with the specified type; otherwise, zero.</returns>
    public FixedPoint2 GetValueOrDefault(ProtoId<DamageTypePrototype> key)
    {
        return Damage.GetValueOrDefault(key);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="Damage"/>.
    /// </summary>
    /// <returns>An Enumerator structure for the <see cref="Damage"/>.</returns>
    public IEnumerator<KeyValuePair<ProtoId<DamageTypePrototype>, FixedPoint2>> GetEnumerator()
    {
        return Damage.GetEnumerator();
    }

    /// <summary>
    /// Returns a string that represents <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <returns>A string that represents <see cref="DamageSpecifier"/>.</returns>
    public override string ToString()
    {
        return $"DamageSpecifier({string.Join("; ", Damage.Select(x => x.Key + ":" + x.Value))})";
    }

    /// <summary>
    /// Returns a new <see cref="DamageSpecifier"/> with applied damage modifier set.
    /// </summary>
    /// <returns>A new <see cref="DamageSpecifier"/> with applied damage modifier set.</returns>
    public static DamageSpecifier ApplyModifierSet(DamageSpecifier specifier, DamageModifierSet modifierSet)
    {
        DamageSpecifier newSpecifier = new();
        newSpecifier.Damage.EnsureCapacity(specifier.Damage.Count);

        foreach (var (type, damage) in specifier.Damage)
        {
            if (damage == 0)
                continue;

            if (damage < 0)
            {
                newSpecifier.Damage[type] = damage;
                continue;
            }

            var newDamage = damage;

            if (modifierSet.FlatReduction.TryGetValue(type, out var reduction))
                newDamage = FixedPoint2.Max(0f, newDamage - reduction);

            if (modifierSet.Coefficients.TryGetValue(type, out var coefficient))
                newDamage *= coefficient;

            if (newDamage == 0)
                continue;

            newSpecifier.Damage[type] = newDamage;
        }

        return newSpecifier;
    }

    /// <summary>
    /// Returns a new <see cref="DamageSpecifier"/> with applied damage modifier sets.
    /// </summary>
    /// <returns>A new <see cref="DamageSpecifier"/> with applied damage modifier sets.</returns>
    public static DamageSpecifier ApplyModifierSets(DamageSpecifier specifier, IEnumerable<DamageModifierSet> modifierSets)
    {
        var newSpecifier = new DamageSpecifier(specifier);

        foreach (var modifierSet in modifierSets)
        {
            newSpecifier = ApplyModifierSet(newSpecifier, modifierSet);
        }

        return newSpecifier;
    }

    /// <summary>
    /// Returns new specifier that only contains the entry with negative value.
    /// </summary>
    /// <returns>A new specifier that only contains the entries with negative value.</returns>
    public static DamageSpecifier GetNegative(DamageSpecifier specifier)
    {
        DamageSpecifier newSpecifier = new();

        foreach (var (type, damage) in specifier.Damage)
        {
            if (damage >= 0)
                continue;

            newSpecifier.Damage[type] = damage;
        }

        return newSpecifier;
    }

    /// <summary>
    /// Returns a new <see cref="DamageSpecifier"/> that only contains the entries with positive value.
    /// </summary>
    /// <returns>A new <see cref="DamageSpecifier"/> that only contains the entries with positive value.</returns>
    public static DamageSpecifier GetPositive(DamageSpecifier specifier)
    {
        DamageSpecifier newSpecifier = new();

        foreach (var (type, damage) in specifier.Damage)
        {
            if (damage <= 0)
                continue;

            newSpecifier.Damage[type] = damage;
        }

        return newSpecifier;
    }

    /// <summary>
    /// Adds an item to the <see cref="Damage"/>.
    /// </summary>
    public void Add(KeyValuePair<ProtoId<DamageTypePrototype>, FixedPoint2> keyValue)
    {
        Damage.Add(keyValue.Key, keyValue.Value);
    }

    /// <summary>
    /// Adds an item to the <see cref="Damage"/>.
    /// </summary>
    public void Add(ProtoId<DamageTypePrototype> key, FixedPoint2 value)
    {
        Damage.Add(key, value);
    }

    /// <summary>
    /// This adds the damage values of some other <see cref="DamageSpecifier"/> to the current one without
    /// adding any new damage types.
    /// </summary>
    public void AddSpecifier(DamageSpecifier other)
    {
        foreach (var (type, damage) in other.Damage)
        {
            if (!Damage.TryGetValue(type, out var existing))
                continue;

            Damage[type] = existing + damage;
        }
    }

    /// <summary>
    /// Clamps each damage value to be within the given range.
    /// </summary>
    public void Clamp(FixedPoint2 min, FixedPoint2 max)
    {
        DebugTools.Assert(min < max);

        ClampMax(max);
        ClampMin(min);
    }

    /// <summary>
    /// Sets all damage values to be at most some number.
    /// </summary>
    public void ClampMax(FixedPoint2 max)
    {
        foreach (var (type, damage) in Damage)
        {
            if (damage <= max)
                continue;

            Damage[type] = max;
        }
    }

    /// <summary>
    /// Sets all damage values to be at least as large as the given number.
    /// </summary>
    public void ClampMin(FixedPoint2 min)
    {
        foreach (var (type, damage) in Damage)
        {
            if (damage >= min)
                continue;

            Damage[type] = damage;
        }
    }

    /// <summary>
    /// Removes all keys and values from the <see cref="Damage"/>.
    /// </summary>
    public void Clear()
    {
        Damage.Clear();
    }

    /// <summary>
    /// Copies the elements of the <see cref="Damage"/> to an Array, starting at a particular index.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="Damage"/>.</param>
    /// <param name="index">The zero-based index in <see cref="Array"/> at which copying begins.</param>
    public void CopyTo(KeyValuePair<ProtoId<DamageTypePrototype>, FixedPoint2>[] array, int index)
    {
        ((ICollection<KeyValuePair<ProtoId<DamageTypePrototype>, FixedPoint2>>)Damage).CopyTo(array, index);
    }

    /// <summary>
    /// Sets all negative damage values to zero.
    /// </summary>
    public void RemoveNegative()
    {
        foreach (var (type, damage) in Damage)
        {
            if (damage >= 0)
                continue;

            Damage[type] = FixedPoint2.Zero;
        }
    }

    /// <summary>
    /// Remove any damage entries with zero damage.
    /// </summary>
    public void TrimZeros()
    {
        foreach (var (type, damage) in Damage)
        {
            if (damage != 0)
                continue;

            Damage.Remove(type);
        }
    }

    #region Operators

    public static DamageSpecifier operator *(DamageSpecifier left, FixedPoint2 right)
    {
        DamageSpecifier newSpecifier = new();

        foreach (var (type, damage) in left.Damage)
        {
            newSpecifier.Damage.Add(type, damage * right);
        }

        return newSpecifier;
    }

    public static DamageSpecifier operator *(DamageSpecifier left, float right)
    {
        DamageSpecifier newSpecifier = new();

        foreach (var (type, damage) in left.Damage)
        {
            newSpecifier.Damage.Add(type, damage * right);
        }

        return newSpecifier;
    }

    public static DamageSpecifier operator *(FixedPoint2 left, DamageSpecifier right)
    {
        return right * left;
    }

    public static DamageSpecifier operator *(float left, DamageSpecifier right)
    {
        return right * left;
    }

    public static DamageSpecifier operator +(DamageSpecifier left, DamageSpecifier right)
    {
        DamageSpecifier newSpecifier = new(left);

        foreach (var (type, damage) in right.Damage)
        {
            if (newSpecifier.Damage.TryAdd(type, damage))
                continue;

            newSpecifier.Damage[type] += damage;
        }

        return newSpecifier;
    }

    public static DamageSpecifier operator +(DamageSpecifier specifier)
    {
        return specifier;
    }

    public static DamageSpecifier operator -(DamageSpecifier left, DamageSpecifier right)
    {
        DamageSpecifier newSpecifier = new(left);

        foreach (var (type, damage) in right.Damage)
        {
            if (newSpecifier.Damage.TryAdd(type, -damage))
                continue;

            newSpecifier.Damage[type] -= damage;
        }

        return newSpecifier;
    }

    public static DamageSpecifier operator -(DamageSpecifier specifier)
    {
        return specifier * -1;
    }

    public static DamageSpecifier operator /(DamageSpecifier left, FixedPoint2 right)
    {
        DamageSpecifier newSpecifier = new();

        foreach (var (type, damage) in left.Damage)
        {
            newSpecifier.Damage.Add(type, damage / right);
        }

        return newSpecifier;
    }

    public static DamageSpecifier operator /(DamageSpecifier left, float right)
    {
        DamageSpecifier newSpecifier = new();

        foreach (var (type, damage) in left.Damage)
        {
            newSpecifier.Damage.Add(type, damage / right);
        }

        return newSpecifier;
    }

    #endregion
}
