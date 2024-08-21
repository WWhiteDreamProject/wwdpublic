using Content.Server.GameTicking;
using Content.Server._White.AspectsSystem.Base;
using Content.Shared._White;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.AspectsSystem.Managers
{
    /// <summary>
    /// Manager for aspects.
    /// </summary>
    public sealed class AspectManager : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private ISawmill _sawmill = default!;

        private bool AspectsEnabled { get; set; }

        private double Chance { get; set; }

        private string? ForcedAspect { get; set; }

        private void SetEnabled(bool value) => AspectsEnabled = value;

        private void SetChance(double value) => Chance = value;

        private void SetForcedAspect(string? value) => ForcedAspect = value;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("aspects");

            _cfg.OnValueChanged(WhiteCVars.IsAspectsEnabled, SetEnabled, true);
            _cfg.OnValueChanged(WhiteCVars.AspectChance, SetChance, true);

            SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        }

        #region Handlers

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            if (!AspectsEnabled)
                return;

            if (ForcedAspect != null)
            {
                RunAspect(ForcedAspect);

                SetForcedAspect(null);

                return;
            }

            if (_random.NextDouble() <= Chance)
                RunRandomAspect();
        }

        #endregion

        #region PublicApi

        /// <summary>
        /// Forces a specific aspect by its prototype ID.
        /// </summary>
        /// <param name="aspectProtoId">The prototype ID of the aspect to be forced.</param>
        public string ForceAspect(string aspectProtoId)
        {
            if (!AspectsEnabled)
            {
                var disabledStr = "Aspects disabled.";
                _sawmill.Warning("Someone tried to force aspect when they disabled!");
                return disabledStr;
            }

            if (!_prototype.TryIndex<EntityPrototype>(aspectProtoId, out var entityPrototype))
            {
                var response = "Aspect not found. Can`t find proto";
                _sawmill.Warning("Someone tried to force invalid Aspect!");
                return response;
            }

            if (!entityPrototype.TryGetComponent<AspectComponent>(out _))
            {
                var errStr = $"Aspect with ID '{aspectProtoId}' not found or does not have an AspectComponent!";
                _sawmill.Error(errStr);
                return errStr;
            }

            if (ForcedAspect == aspectProtoId)
            {
                var errStr = $"Aspect with ID '{aspectProtoId}' already forced!";
                _sawmill.Error(errStr);
                return errStr;
            }

            SetForcedAspect(aspectProtoId);

            var str = $"Successfully forced Aspect with ID '{aspectProtoId}'";
            _sawmill.Info(str);
            return str;
        }

        /// <summary>
        /// DeForces a ForcedAspect, if any.
        /// </summary>
        public string DeForceAspect()
        {
            string response;

            if (ForcedAspect != null)
            {
                response = $"DeForced Aspect : {ForcedAspect}";
                SetForcedAspect(null);
            }
            else
                response = "How to DeForce if no aspect forced, retard..";

            return response;
        }


        /// <summary>
        /// Retrieves information about the currently forced aspect, if any.
        /// </summary>
        public string GetForcedAspect()
        {
            var response = ForcedAspect != null
                ? $"Current forced Aspect : {ForcedAspect}"
                : "No forced Aspects";

            return response;
        }

        /// <summary>
        /// Retrieves a list of IDs for all available aspects.
        /// </summary>
        /// <returns>A list of IDs for available aspects.</returns>
        public List<string> GetAllAspectIds()
        {
            var availableAspects = AllAspects();
            var aspectIds = new List<string>();

            foreach (var (proto, aspect) in availableAspects)
            {
                var initialAspectId = proto.ID;
                var returnedAspectId = proto.ID;

                if (aspect.Requires != null)
                    returnedAspectId += $" (Requires: {aspect.Requires})";

                if (aspect.IsForbidden)
                    returnedAspectId += " (ShitSpawn)";

                if (ForcedAspect == initialAspectId)
                    returnedAspectId += " (Forced)";

                if (CheckIfAspectAlreadyRunning(initialAspectId))
                    returnedAspectId += " (Already Running)";

                aspectIds.Add(returnedAspectId);
            }

            return aspectIds;
        }

        /// <summary>
        /// Runs the specified aspect and adds it as a game rule.
        /// </summary>
        /// <param name="aspectId">The ID of the aspect to run.</param>
        public string RunAspect(string aspectId)
        {
            if (!AspectsEnabled)
            {
                var disabledStr = "Aspects disabled.";
                _sawmill.Warning("Someone tried to run aspects when they disabled!");
                return disabledStr;
            }

            if (!_prototype.TryIndex<EntityPrototype>(aspectId, out var entityPrototype))
            {
                var response = "Aspect not found. Can`t find proto";
                _sawmill.Warning("Someone tried to run invalid Aspect!");
                return response;
            }

            if (!entityPrototype.TryGetComponent<AspectComponent>(out var aspect))
            {
                var errStr = $"Aspect with ID '{aspectId}' not found or does not have an AspectComponent!";
                _sawmill.Error(errStr);
                return errStr;
            }

            if (CheckIfAspectAlreadyRunning(aspectId))
            {
                var alreadyRunningStr = $"Aspect '{aspectId}' is already running!";
                _sawmill.Warning(alreadyRunningStr);
                return alreadyRunningStr;
            }

            var ent = _gameTicker.AddGameRule(aspectId);
            var str = $"Ran {aspect.Name ?? "Unnamed Aspect"} ({ToPrettyString(ent)})!!";
            _sawmill.Info(str);
            return str;
        }

        /// <summary>
        /// Runs a random aspect and adds it as a game rule.
        /// </summary>
        public string RunRandomAspect()
        {
            if (!AspectsEnabled)
            {
                var disabledStr = "Aspects disabled.";
                _sawmill.Warning("Someone tried to run aspects when they disabled!");
                return disabledStr;
            }

            var randomAspect = PickRandomAspect();

            if (randomAspect == null)
            {
                var errStr = "Oopsie, no valid aspects found! Sorry.";
                _sawmill.Error(errStr);
                return errStr;
            }

            var ent = _gameTicker.AddGameRule(randomAspect);
            var str = $"Ran {ToPrettyString(ent)}!!";
            _sawmill.Info(str);
            return str;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Picks a random aspect based on their weight.
        /// </summary>
        /// <param name="allowForbidden">Allow selecting forbidden aspects.</param>
        /// <returns>The ID of the selected aspect or null if no aspect was selected.</returns>
        private string? PickRandomAspect(bool allowForbidden = false)
        {
            var availableAspects = AllAspects();
            _sawmill.Info($"Picking from {availableAspects.Count} total available aspects");
            return FindAspect(availableAspects, allowForbidden);
        }

        /// <summary>
        /// Finds a suitable aspect from the available aspects.
        /// </summary>
        /// <param name="availableAspects">A dictionary of available aspects.</param>
        /// <param name="allowForbidden">Allow selecting forbidden aspects.</param>
        /// <returns>The ID of the selected aspect or null if no aspect was found.</returns>
        private string? FindAspect(Dictionary<EntityPrototype, AspectComponent> availableAspects, bool allowForbidden = false)
        {
            if (availableAspects.Count == 0)
            {
                _sawmill.Warning("No aspects were available to run!");
                return null;
            }

            var sumOfWeights = 0;

            foreach (var (_, aspect) in availableAspects)
            {
                if (!allowForbidden && aspect.IsForbidden)
                    continue;

                sumOfWeights += (int)aspect.Weight;
            }

            sumOfWeights = _random.Next(sumOfWeights);

            foreach (var (proto, aspect) in availableAspects)
            {
                if (!allowForbidden && aspect.IsForbidden)
                    continue;

                if (CheckIfAspectAlreadyRunning(proto.ID))
                    continue;

                sumOfWeights -= (int)aspect.Weight;

                if (sumOfWeights <= 0)
                    return proto.ID;
            }

            _sawmill.Error("Aspect was not found after weighted pick process!");

            return null;
        }


        /// <summary>
        /// Checking if aspect is already running, needed to avoid repeating.
        /// </summary>
        private bool CheckIfAspectAlreadyRunning(string aspectId)
        {
            var activeRules = _gameTicker.GetActiveGameRules();

            foreach (var gameRule in activeRules)
            {
                if (!HasComp<AspectComponent>(gameRule))
                    continue;

                if (!TryComp<MetaDataComponent>(gameRule, out var metaDataComponent))
                    continue;

                var runningAspectId = metaDataComponent.EntityPrototype?.ID;

                if (runningAspectId == aspectId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a dictionary of all available aspects from prototypes.
        /// </summary>
        /// <returns>A dictionary of available aspects.</returns>
        private Dictionary<EntityPrototype, AspectComponent> AllAspects()
        {
            var allAspects = new Dictionary<EntityPrototype, AspectComponent>();
            foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
            {
                if (prototype.Abstract)
                    continue;

                if (!prototype.TryGetComponent<AspectComponent>(out var aspect))
                    continue;

                allAspects.Add(prototype, aspect);
            }

            return allAspects;
        }

        #endregion
    }
}
