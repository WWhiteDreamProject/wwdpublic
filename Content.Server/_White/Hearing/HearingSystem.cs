using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chat;
using Content.Shared.Language;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Timing;


namespace Content.Server._White.Hearing;

public sealed class HearingSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HearingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<HearingComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Remove timed out deafness sources
        var query = EntityQueryEnumerator<HearingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            foreach (var source in component.DeafnessSources.ToList())
            {
                if (!source.Permanent && _timing.CurTime > source.DeafnessTimer)
                {
                    component.DeafnessSources.Remove(source);
                    UpdateDeafnessState(uid, component);
                }
            }
        }
    }

    private void OnMobStateChanged(EntityUid uid, HearingComponent component, MobStateChangedEvent args)
    {
        var source = new DeafnessSource("mobstate", "deaf-chat-message", true);

        if (args.NewMobState == MobState.Alive)
        {
            RemoveDeafnessSource(component, source.Id);
            UpdateDeafnessState(uid, component);
            return;
        }

        component.DeafnessSources.Add(source);
        UpdateDeafnessState(uid, component);
    }

    private void OnSleepStateChanged(EntityUid uid, HearingComponent component, SleepStateChangedEvent args)
    {
        var source = new DeafnessSource("sleeping", "deaf-chat-message", true);

        if (!args.FellAsleep)
        {
            RemoveDeafnessSource(component, source.Id);
            UpdateDeafnessState(uid, component);
            return;
        }

        component.DeafnessSources.Add(source);
        UpdateDeafnessState(uid, component);
    }

    public void UpdateDeafnessState(EntityUid uid, HearingComponent component)
    {
        // Dont do anything if always deaf
        if (TryComp<DeafComponent>(uid, out var deaf) && deaf.AlwaysDeaf)
            return;

        if (component.DeafnessSources.Count == 0)
        {
            RemComp<DeafComponent>(uid);
            return;
        }

        var message = component.DeafnessSources[0].DeafChatMessage; // Don't think we care about message priority, add if needed

        EnsureComp<DeafComponent>(uid, out var deafComponent);
        deafComponent.DeafChatMessage = message;
    }

    private void RemoveDeafnessSource(HearingComponent component, string id)
    {
        if (component.DeafnessSources.Count <= 0)
            return;

        foreach (var source in component.DeafnessSources.ToList())
            if (source.Id == id)
                component.DeafnessSources.Remove(source);
    }

    // Public API
    // Returns true if the message can not be heard
    // If true, sends a DeafChatMessage in player's chat
    public bool IsBlockedByDeafness(ICommonSession session, ChatChannel channel, LanguagePrototype language)
    {
        if (channel is not (ChatChannel.Local or ChatChannel.Whisper or ChatChannel.Radio or ChatChannel.Notifications))
            return false;

        if (!language.SpeechOverride.RequireSpeech) // Non-verbal languages e.g. sign language
            return false;

        if (TryComp<DeafComponent>(session.AttachedEntity, out var deafComp))
        {
            var message = Loc.GetString(deafComp.DeafChatMessage);
            var wrappedMessage = $"{message}";

            _chatManager.ChatMessageToOne(ChatChannel.Local, message, wrappedMessage, EntityUid.Invalid, false, session.Channel);
            return true;
        }

        return false;
    }
}

public sealed class DeafnessSource
{
    public string Id;
    public string DeafChatMessage;
    public bool Permanent;
    public TimeSpan DeafnessTimer;

    public DeafnessSource(string id, string deafChatMessage, bool permanent, TimeSpan timer = default)
    {
        Id = id;
        DeafChatMessage = deafChatMessage;
        Permanent = permanent;
        DeafnessTimer = timer;
    }
}
