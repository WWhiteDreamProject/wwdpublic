using System;
using Robust.Shared.Localization;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Content.Shared.Mind.Components;
using Robust.Shared.Timing;

namespace Content.Server._NC.Forensics;

public sealed class NeuralPortBufferSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, ComponentInit>(OnMindInit);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);

        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnMindInit(EntityUid uid, MindContainerComponent component, ComponentInit args)
    {
        EnsureComp<NeuralPortBufferComponent>(uid);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!TryComp<NeuralPortBufferComponent>(args.Target, out var buffer))
            return;

        buffer.TimeOfDeath = _timing.CurTime;
    }

    private void OnDamageChanged(EntityUid uid, DamageableComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (!TryComp<NeuralPortBufferComponent>(uid, out var buffer))
            return;

        var maxType = args.DamageDelta.DamageDict
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(maxType))
            return;

        buffer.LastCriticalDamage = MapDamageTypeToCategory(maxType);
    }

    private string MapDamageTypeToCategory(string damageType)
    {
        // Basic mapping for forensic summary.
        return damageType switch
        {
            "Heat" or "Cold" or "Shock" => Loc.GetString("forensics-damage-thermal"),
            "Slash" => Loc.GetString("forensics-damage-slash"),
            "Piercing" or "Blunt" => Loc.GetString("forensics-damage-firearm"),
            _ => Loc.GetString("forensics-damage-unknown")
        };
    }

    private void OnEntitySpoke(EntitySpokeEvent ev)
    {
        if (ev.Channel != null)
            return;

        var range = ev.IsWhisper ? ChatSystem.WhisperMuffledRange : ChatSystem.VoiceRange;
        AddLineToNearby(ev.Source, ev.Message, range);
    }

    private void AddLineToNearby(EntityUid speaker, string message, float range)
    {
        var coords = Transform(speaker).Coordinates;
        var entities = _lookup.GetEntitiesInRange(coords, range, LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate);
        var time = _timing.CurTime;
        var speakerName = Name(speaker);

        foreach (var uid in entities)
        {
            if (!TryComp<NeuralPortBufferComponent>(uid, out var buffer))
                continue;

            var line = new NeuralPortLogLine(time, speakerName, message, speaker == uid);
            buffer.Lines.Insert(0, line);
            while (buffer.Lines.Count > buffer.MaxLines)
                buffer.Lines.RemoveAt(buffer.Lines.Count - 1);
        }
    }
}












