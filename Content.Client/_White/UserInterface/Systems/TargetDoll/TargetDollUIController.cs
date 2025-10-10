using Content.Client.Gameplay;
using Content.Client._White.TargetDoll;
using Content.Client._White.UserInterface.Systems.TargetDoll.Widgets;
using Content.Shared._White.TargetDoll;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._White.UserInterface.Systems.TargetDoll;

public sealed class TargetDollUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetDollSystem>
{
    private TargetDollComponent? _targetingComponent;
    private TargetDollGui? TargetDollGui => UIManager.GetActiveUIWidgetOrNull<TargetDollGui>();

    public void OnSystemLoaded(TargetDollSystem system)
    {
        system.LocalPlayerTargetDollUpdated += OnTargetDollUpdated;
        system.LocalPlayerTargetDollAdded += OnTargetDollAdded;
        system.LocalPlayerTargetDollRemoved += OnTargetDollRemoved;
    }

    public void OnSystemUnloaded(TargetDollSystem system)
    {
        system.LocalPlayerTargetDollUpdated -= OnTargetDollUpdated;
        system.LocalPlayerTargetDollAdded -= OnTargetDollAdded;
        system.LocalPlayerTargetDollRemoved -= OnTargetDollRemoved;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (TargetDollGui == null)
            return;

        TargetDollGui.Visible = _targetingComponent != null;
    }

    private void OnTargetDollUpdated(BodyPart bodyPart) => TargetDollGui?.OnTargetDollUpdated(bodyPart);

    private void OnTargetDollAdded(TargetDollComponent component)
    {
        if (TargetDollGui != null)
            TargetDollGui.Visible = true;

        _targetingComponent = component;
        OnTargetDollUpdated(component.SelectedBodyPart);
    }

    private void OnTargetDollRemoved()
    {
        if (TargetDollGui != null)
            TargetDollGui.Visible = false;

        _targetingComponent = null;
    }

    public void SelectBodyPart(BodyPart bodyPart) =>
        EntityManager.RaisePredictiveEvent(new SelectBodyPartRequestEvent(bodyPart));
}
