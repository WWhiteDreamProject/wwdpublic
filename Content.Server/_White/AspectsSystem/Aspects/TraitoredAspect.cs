using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server._White.AspectsSystem.Managers;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._White.AspectsSystem.Aspects
{
    public sealed class TraitoredAspect : AspectSystem<TraitoredAspectComponent>
    {
        [Dependency] private readonly TraitorRuleSystem _traitorRuleSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AspectManager _aspectManager = default!;

        private TraitorRuleComponent? _traitorRuleComponent;

        private bool _announcedForTraitors;

        private float _time;
        private float _timeElapsedForTraitors;

        protected override void Started(EntityUid uid, TraitoredAspectComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            // Just to make sure
            ResetValues();

            _traitorRuleComponent = GetTraitorGameRule();
            if (_traitorRuleComponent == null)
                _aspectManager.RunRandomAspect();

            _timeElapsedForTraitors = _random.Next(component.TimeElapsedForAllMin, component.TimeElapsedForAllMax);
        }

        protected override void ActiveTick(EntityUid uid, TraitoredAspectComponent component, GameRuleComponent gameRule, float frameTime)
        {
            base.ActiveTick(uid, component, gameRule, frameTime);

            if (_traitorRuleComponent?.SelectionStatus != TraitorRuleComponent.SelectionState.Started)
                return;

            _time += frameTime;

            if (_time >= component.TimeElapsedForTraitors && !_announcedForTraitors)
            {
                AnnounceToTraitors(uid, gameRule, component.AnnouncementForTraitorSound);
                _announcedForTraitors = true;
            }

            if (_time >= _timeElapsedForTraitors)
                AnnounceToAll(uid, gameRule);

        }

        protected override void Ended(EntityUid uid, TraitoredAspectComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);
            ResetValues();
        }

        #region Helpers

        private void AnnounceToTraitors(EntityUid uid, GameRuleComponent rule, string sound)
        {
            var traitors = _traitorRuleSystem.GetAllLivingConnectedTraitors();

            if (traitors.Count == 0)
                ForceEndSelf(uid, rule);

            foreach (var traitor in traitors)
            {
                if (!_mindSystem.TryGetSession(traitor.Mind, out var session))
                    continue;

                var mindOwned = traitor.Mind.OwnedEntity;

                if (mindOwned == null)
                    return;

                _chatManager.DispatchServerMessage(session, Loc.GetString("aspect-traitored-briefing"));
                _audio.PlayEntity(sound, mindOwned.Value, mindOwned.Value);
            }
        }

        private void AnnounceToAll(EntityUid uid, GameRuleComponent rule)
        {
            var traitors = _traitorRuleSystem.GetAllLivingConnectedTraitors();

            var msg = Loc.GetString("aspect-traitored-announce");

            foreach (var traitor in traitors)
            {
                var name = traitor.Mind.CharacterName;
                if (!string.IsNullOrEmpty(name))
                    msg += $"\n + {Loc.GetString("aspect-traitored-announce-name", ("name", name))}";
            }

            _chatSystem.DispatchGlobalAnnouncement(msg, Loc.GetString("aspect-traitored-announce-sender"), colorOverride: Color.Aquamarine);

            ForceEndSelf(uid, rule);
        }

        private void ResetValues()
        {
            _announcedForTraitors = false;
            _time = 0;
        }

        private TraitorRuleComponent? GetTraitorGameRule()
        {
            if (EntityQuery<TraitorRuleComponent>().Any())
                return EntityQuery<TraitorRuleComponent>().First();

            return null;
        }

        #endregion

    }
}
