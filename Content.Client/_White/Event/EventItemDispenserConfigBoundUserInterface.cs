using Content.Client.UserInterface.Controls;
using Content.Shared._White.Event;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.Event;

#pragma warning disable IDE1006 // i am going to fucking lose it

/// <summary>
/// Hopefully i never have to touch UI ever again.
/// Even if xaml thing was working for me, this would only be marginally less of a radioactive dump.
/// </summary>
public sealed class EventItemDispenserConfigBoundUserInterface : BoundUserInterface
{

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    static readonly Color Green = new Color(0, 255, 0);
    static readonly Color Red = new Color(255, 0, 0);
    static readonly Color Gray = new Color(127, 127, 127);

    //EventItemDispenserConfigWindow? window; // Trying to work with robustengine's ui system makes me want to quote AM.
    DefaultWindow window = default!;
    EventItemDispenserComponent dispenserComp;
    public EventItemDispenserConfigBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {
        IoCManager.InjectDependencies(this);
        dispenserComp = _entMan.GetComponent<EventItemDispenserComponent>(Owner);
    }

    #region man-made horrors beyond your comprehension 

    #region all the controls
    BoxContainer baseBox = default!;
    BoxContainer optionBox = default!;
    BoxContainer buttonBox = default!;
    BoxContainer copypasteBox = default!;

    LineEdit DispensingPrototypeLineEdit = default!;
    bool DispensingPrototypeValid = false;
    CheckBox AutoDisposeCheckBox = default!;
    CheckBox CanManuallyDisposeCheckBox = default!;
    CheckBox InfiniteCheckBox = default!;
    LineEdit LimitLineEdit = default!;

    CheckBox ReplaceDisposedItemsCheckBox = default!;
    LineEdit DisposedReplacementPrototypeLineEdit = default!;
    bool DisposedReplacementPrototypeValid = false;

    CheckBox AutoCleanUpCheckBox = default!;

    Button copyButton = default!;
    Button pasteButton = default!;
    Button confirmButton = default!;

    //Button cancelButton = new(); // just hit the "x" 4head
    #endregion

    #region copypasta stuff
    static string SavedDispensingPrototype = default!;
    static bool SavedAutoDispose = default;
    static bool SavedCanManuallyDispose = default;
    static bool SavedInfinite = default;
    static string SavedLimit = default!;
    static bool SavedReplaceDisposedItems = default;
    static string SavedDisposedReplacementPrototype = default!;
    static bool SavedAutoCleanUp = default;

    static bool saved = false;

    private void CopySettings(EventArgs whatever)
    {
        saved = true;

        SavedDispensingPrototype = DispensingPrototypeLineEdit.Text;
        SavedAutoDispose = AutoDisposeCheckBox.Pressed;
        SavedCanManuallyDispose = CanManuallyDisposeCheckBox.Pressed;
        SavedInfinite = InfiniteCheckBox.Pressed;
        SavedLimit = LimitLineEdit.Text;
        SavedReplaceDisposedItems = ReplaceDisposedItemsCheckBox.Pressed;
        SavedDisposedReplacementPrototype = DisposedReplacementPrototypeLineEdit.Text;
        SavedAutoCleanUp = AutoCleanUpCheckBox.Pressed;

        pasteButton.Disabled = false;
    }

    private void PasteSettings(EventArgs whatever)
    {
        DebugTools.Assert(saved);

        DispensingPrototypeLineEdit.Text = SavedDispensingPrototype;
        AutoDisposeCheckBox.Pressed = SavedAutoDispose;
        CanManuallyDisposeCheckBox.Pressed = SavedCanManuallyDispose;
        InfiniteCheckBox.Pressed = SavedInfinite;
        LimitLineEdit.Text = SavedLimit;
        ReplaceDisposedItemsCheckBox.Pressed = SavedReplaceDisposedItems;
        DisposedReplacementPrototypeLineEdit.Text = SavedDisposedReplacementPrototype;
        AutoCleanUpCheckBox.Pressed = SavedAutoCleanUp;
        
    }
    #endregion

    private void InitializeControls(DefaultWindow window) // windows forms ahh method 
    {
        baseBox = this.CreateDisposableControl<BoxContainer>();
        baseBox.Orientation = BoxContainer.LayoutOrientation.Vertical;
        baseBox.SeparationOverride = 4;
        baseBox.Margin = new Thickness(4, 0);
        baseBox.MinWidth = 450;
        window.Contents.AddChild(baseBox);

        optionBox = this.CreateDisposableControl<BoxContainer>();
        optionBox.Orientation = BoxContainer.LayoutOrientation.Vertical;
        baseBox.AddChild(optionBox);

        buttonBox = this.CreateDisposableControl<BoxContainer>();
        buttonBox.Orientation = BoxContainer.LayoutOrientation.Horizontal;
        //buttonBox.Align = BoxContainer.AlignMode.End;
        baseBox.AddChild(buttonBox);

        copypasteBox = this.CreateDisposableControl<BoxContainer>();
        //copypasteBox.HorizontalAlignment = Control.HAlignment.Left;
        copypasteBox.HorizontalExpand = true;

        copyButton = this.CreateDisposableControl<Button>();
        copyButton.Label.Text = "Copy";
        copyButton.OnPressed += CopySettings;
        copypasteBox.AddChild(copyButton);

        pasteButton = this.CreateDisposableControl<Button>();
        pasteButton.Label.Text = "Paste";
        pasteButton.OnPressed += PasteSettings;
        pasteButton.Disabled = !saved;
        copypasteBox.AddChild(pasteButton);

        confirmButton = this.CreateDisposableControl<Button>();
        confirmButton.Label.Text = "OK";
        confirmButton.OnPressed += TrySubmit;
        confirmButton.HorizontalAlignment = Control.HAlignment.Right;

        buttonBox.AddChild(copypasteBox);
        buttonBox.AddChild(confirmButton);

    }





    /// <summary>
    /// I am not sorry.
    /// </summary>
    protected override void Open() // pure aids
    {
        base.Open(); // what's the fucking point?
        window = this.CreateDisposableControl<DefaultWindow>();
        window.Resizable = false;
        window.OnClose += Close;
        InitializeControls(window);

        window.Title = _loc.GetString("eventitemdispenser-configwindow-title");
        DispensingPrototypeLineEdit = AddOption<LineEdit>("eventitemdispenser-configwindow-dispensingprototype");
        DispensingPrototypeLineEdit.MinWidth = 300;
        DispensingPrototypeLineEdit.OnTextChanged += (args) => { DispensingPrototypeValid = ValidateProto(args); confirmButton!.Disabled = !DispensingPrototypeValid || !DisposedReplacementPrototypeValid; };
        DispensingPrototypeLineEdit.OnTextEntered += TrySubmit;
        

        AutoDisposeCheckBox = AddOption<CheckBox>("eventitemdispenser-configwindow-autodispose");
        CanManuallyDisposeCheckBox = AddOption<CheckBox>("eventitemdispenser-configwindow-canmanuallydispose");

        InfiniteCheckBox = AddOption<CheckBox>("eventitemdispenser-configwindow-infinite");
        InfiniteCheckBox.OnPressed += (_) => {
            AutoDisposeCheckBox.Disabled = !InfiniteCheckBox.Pressed;
            AutoDisposeCheckBox.ModulateSelfOverride = AutoDisposeCheckBox.Disabled ? Gray : null;
        };

        LimitLineEdit = AddOption<LineEdit>("eventitemdispenser-configwindow-limit");
        LimitLineEdit.IsValid = s => int.TryParse(s, out int _) && s.IndexOf('-') == -1; // no "_ > 0" because being able to input -0 makes me cringe
        LimitLineEdit.MinWidth = 100;
        LimitLineEdit.OnTextEntered += TrySubmit;

        ReplaceDisposedItemsCheckBox = AddOption<CheckBox>("eventitemdispenser-configwindow-replacedisposeditems");

        DisposedReplacementPrototypeLineEdit = AddOption<LineEdit>("eventitemdispenser-configwindow-disposedreplacement");
        DisposedReplacementPrototypeLineEdit.MinWidth = 300;
        DisposedReplacementPrototypeLineEdit.OnTextChanged += (args) => { DisposedReplacementPrototypeValid = ValidateProto(args); confirmButton!.Disabled = !DispensingPrototypeValid || !DisposedReplacementPrototypeValid; };
        DisposedReplacementPrototypeLineEdit.OnTextEntered += TrySubmit;

        AutoCleanUpCheckBox = AddOption<CheckBox>("eventitemdispenser-configwindow-autocleanup");

        
        DispensingPrototypeLineEdit.SetText(dispenserComp.DispensingPrototype, true);
        AutoDisposeCheckBox.Pressed = dispenserComp.AutoDispose;
        CanManuallyDisposeCheckBox.Pressed = dispenserComp.CanManuallyDispose;
        InfiniteCheckBox.Pressed = dispenserComp.Infinite;
        LimitLineEdit.SetText(dispenserComp.Limit.ToString());
        ReplaceDisposedItemsCheckBox.Pressed = dispenserComp.ReplaceDisposedItems;
        DisposedReplacementPrototypeLineEdit.SetText(dispenserComp.DisposedReplacement, true);
        AutoCleanUpCheckBox.Pressed = dispenserComp.AutoCleanUp;

        AutoDisposeCheckBox.Disabled = !dispenserComp.Infinite;

        window.OpenCentered();
    }


    private T AddOption<T>(string text) where T : Control, IDisposable, new()
    {
        var box = this.CreateDisposableControl<BoxContainer>();
        box.HorizontalAlignment = Control.HAlignment.Stretch;
        var label = this.CreateDisposableControl<Label>();
        label.Text = _loc.GetString(text);
        label.HorizontalExpand = true;
        label.HorizontalAlignment = Label.HAlignment.Left;
        box.AddChild(label);
        var control = this.CreateDisposableControl<T>();
        control.HorizontalAlignment = Control.HAlignment.Right;
        control.ToolTip = _loc.GetString($"{text}-tooltip");
        box.AddChild(control);
        optionBox!.AddChild(box); // called after control init // i can't even remember what i meant by this, that's how bad it has got.
        return control;
    }
    private bool ValidateProto(LineEdit.LineEditEventArgs args)
    {
        bool val = _proto.HasIndex(args.Text);
        args.Control.ModulateSelfOverride = val ? Green : Red;
        return val;
    }

    private void TrySubmit(EventArgs whatever)
    {
        if (!confirmButton.Disabled)
        {
            var msg = new EventItemDispenserNewConfigBoundUserInterfaceMessage()
            {
                DispensingPrototype = DispensingPrototypeLineEdit.Text,
                AutoDispose = AutoDisposeCheckBox.Pressed,
                CanManuallyDispose = CanManuallyDisposeCheckBox.Pressed,
                Infinite = InfiniteCheckBox.Pressed,
                Limit = int.Parse(LimitLineEdit.Text),
                ReplaceDisposedItems = ReplaceDisposedItemsCheckBox.Pressed,
                DisposedReplacement = DisposedReplacementPrototypeLineEdit.Text,
                AutoCleanUp = AutoCleanUpCheckBox.Pressed
            };
            SendMessage(msg);
            Close();
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if(message is EventItemDispenserNewProtoBoundUserInterfaceMessage { } msg)
        {
            DispensingPrototypeLineEdit.SetText(msg.DispensingPrototype);
        }
    }
    #endregion
}
#pragma warning restore IDE1006
