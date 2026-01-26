using Content.Shared._NC.Netrunning.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._NC.Netrunning.UI;

[UsedImplicitly]
public sealed class CyberdeckBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CyberdeckWindow? _window;

    public CyberdeckBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new CyberdeckWindow();
        _window.OnClose += Close;
        _window.OnProgramSelected += id => SendMessage(new CyberdeckProgramRequestMessage(id));
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CyberdeckBoundUiState cast || _window == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var programs = new List<(string, NetEntity, NetProgramData)>();

        if (cast.Programs != null)
        {
            foreach (var (id, program) in cast.Programs)
            {
                var uid = entMan.GetEntity(id);
                var name = entMan.GetComponent<MetaDataComponent>(uid).EntityName;
                programs.Add((name, id, program));
            }
        }

        _window.UpdateState(cast.CurrentRam, cast.MaxRam, programs, cast.TargetName);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}
