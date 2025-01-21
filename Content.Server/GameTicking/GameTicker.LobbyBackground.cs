using Robust.Shared.Random;
using System.Linq;
using Content.Shared._White;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables]
    public AnimatedLobbyScreenPrototype? AnimatedLobbyScreen { get; private set; } // WD EDIT

    [ViewVariables]
    private List<AnimatedLobbyScreenPrototype> _lobbyBackgrounds = []; // WD EDIT

    private void InitializeLobbyBackground()
    {
        _lobbyBackgrounds = _prototypeManager.EnumeratePrototypes<AnimatedLobbyScreenPrototype>() // WD EDIT
            .ToList();

        RandomizeLobbyBackground();
    }

    private void RandomizeLobbyBackground() => AnimatedLobbyScreen = _lobbyBackgrounds!.Any() ? _robustRandom.Pick(_lobbyBackgrounds!) : null; // WD EDIT
}
