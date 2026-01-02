using System.Linq;
using Content.Shared.Changeling;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Radio;
using Robust.Shared.Random;
using Robust.Shared.Log;
using Robust.Shared.Localization;

namespace Content.Server.Changeling;

public sealed class ChangelingHivemindSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private const string HivemindChannelId = "Hivemind";

    private readonly string[] _changelingNameKeys =
    {
        "changeling-hivemind-name-alpha",
        "changeling-hivemind-name-beta",
        "changeling-hivemind-name-gamma",
        "changeling-hivemind-name-delta",
        "changeling-hivemind-name-epsilon",
        "changeling-hivemind-name-zeta",
        "changeling-hivemind-name-eta",
        "changeling-hivemind-name-theta",
        "changeling-hivemind-name-iota",
        "changeling-hivemind-name-kappa",
        "changeling-hivemind-name-lambda",
        "changeling-hivemind-name-mu",
        "changeling-hivemind-name-nu",
        "changeling-hivemind-name-xi",
        "changeling-hivemind-name-omicron",
        "changeling-hivemind-name-pi",
        "changeling-hivemind-name-rho",
        "changeling-hivemind-name-sigma",
        "changeling-hivemind-name-tau",
        "changeling-hivemind-name-upsilon",
        "changeling-hivemind-name-phi",
        "changeling-hivemind-name-chi",
        "changeling-hivemind-name-psi",
        "changeling-hivemind-name-omega"
    };

    private readonly HashSet<string> _usedNameKeys = new();
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("changeling.hivemind");

        SubscribeLocalEvent<ChangelingComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
        SubscribeLocalEvent<HivemindComponent, ComponentStartup>(OnHivemindStartup);
    }

    private void OnSpeakerNameTransform(EntityUid uid, ChangelingComponent component, ref TransformSpeakerNameEvent args)
    {
        if (args.Channel?.ID != HivemindChannelId)
            return;

        if (!TryComp<ChangelingHivemindNameComponent>(uid, out var nameComp))
        {
            _sawmill.Error($"Changeling {ToPrettyString(uid)} lacks ChangelingHivemindNameComponent");
            return;
        }

        if (string.IsNullOrEmpty(nameComp.HivemindName))
        {
            EnsureChangelingHivemindName(uid);
            if (string.IsNullOrEmpty(nameComp.HivemindName))
            {
                _sawmill.Error($"Failed to assign hivemind name to changeling {ToPrettyString(uid)}");
                return;
            }
        }

        args.VoiceName = Loc.GetString(nameComp.HivemindName);
        _sawmill.Debug($"Transformed speaker name for {ToPrettyString(uid)} to {args.VoiceName} in hivemind channel");
    }

    private void OnHivemindStartup(EntityUid uid, HivemindComponent component, ComponentStartup args)
    {
        if (HasComp<ChangelingComponent>(uid))
            EnsureChangelingHivemindName(uid);
    }

    public void EnsureChangelingHivemindName(EntityUid uid)
    {
        var nameComp = EnsureComp<ChangelingHivemindNameComponent>(uid);

        if (!string.IsNullOrEmpty(nameComp.HivemindName))
            return;

        var availableNameKeys = _changelingNameKeys.Except(_usedNameKeys).ToList();

        if (availableNameKeys.Count == 0)
        {
            var nameKey = _random.Pick(_changelingNameKeys);
            nameComp.HivemindName = nameKey;
            _sawmill.Warning($"No unique hivemind names available, assigning potentially duplicate name key {nameKey} to {ToPrettyString(uid)}");
        }
        else
        {
            var newNameKey = _random.Pick(availableNameKeys);
            _usedNameKeys.Add(newNameKey);
            nameComp.HivemindName = newNameKey;
            _sawmill.Debug($"Assigned hivemind name key {newNameKey} to {ToPrettyString(uid)}");
        }

        Dirty(uid, nameComp);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _usedNameKeys.Clear();
    }
}
