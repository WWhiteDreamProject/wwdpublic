using Content.Client.UserInterface.Controls;
using Content.Shared._White.Event;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.Event;


/// <summary>
/// Hopefully i never have to touch UI ever again.
/// Even if xaml thing was working for me, this would only be marginally less of a radioactive dump.
/// </summary>
public sealed class EventItemDispenserConfigBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    
    //EventItemDispenserConfigWindow? window; // Trying to work with robustengine's ui system makes me want to quote AM.
    DefaultWindow? window;
    EventItemDispenserComponent dispenserComp;
    public EventItemDispenserConfigBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {
        IoCManager.InjectDependencies(this);
        dispenserComp = _entMan.GetComponent<EventItemDispenserComponent>(Owner);
    }

    BoxContainer? baseBox;
    BoxContainer? optionBox;
    BoxContainer? buttonBox;

    LineEdit? DispensingPrototypeLineEdit;
    bool DispensingPrototypeValid = false;
    CheckBox? AutoDisposeCheckBox;
    CheckBox? CanManuallyDisposeCheckBox;
    CheckBox? InfiniteCheckBox;
    LineEdit? LimitLineEdit;

    CheckBox? ReplaceDisposedItemsCheckBox;
    LineEdit? DisposedReplacementLineEdit;
    bool DisposedReplacementPrototypeValid = false;

    CheckBox? AutoCleanUpCheckBox;

    Button? copyButton;
    Button? pasteButton;
    Button? confirmButton;

    //Button cancelButton = new(); // just hit the "x" 4head

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
        buttonBox.Align = BoxContainer.AlignMode.End;
        baseBox.AddChild(buttonBox);

        confirmButton = this.CreateDisposableControl<Button>();
        confirmButton.Label.Text = "OK";
        buttonBox.AddChild(confirmButton);
    }


    Color Green = new Color(0, 255, 0);
    Color Red = new Color(255, 0, 0);


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
        

        DispensingPrototypeLineEdit =   AddOption<LineEdit>("dispensingPrototypeLineEdit");
        DispensingPrototypeLineEdit.MinWidth = 300;
        DispensingPrototypeLineEdit.OnTextChanged += (args) => { DispensingPrototypeValid = ValidateProto(args); confirmButton!.Disabled = !DispensingPrototypeValid || !DisposedReplacementPrototypeValid; };

        AutoDisposeCheckBox =           AddOption<CheckBox>("AutoDisposeCheckBox");
        CanManuallyDisposeCheckBox =    AddOption<CheckBox>("CanManuallyDisposeCheckBox");

        InfiniteCheckBox =              AddOption<CheckBox>("InfiniteCheckBox");

        LimitLineEdit =                 AddOption<LineEdit>("LimitLineEdit");
        LimitLineEdit.IsValid = s => int.TryParse(s, out int _) && s.IndexOf('-') == -1; // no "_ > 0" because being able to input -0 makes me cringe
        LimitLineEdit.MinWidth = 100;

        ReplaceDisposedItemsCheckBox =  AddOption<CheckBox>("ReplaceDisposedItemsCheckBox");

        DisposedReplacementLineEdit =   AddOption<LineEdit>("DisposedReplacementLineEdit");
        DisposedReplacementLineEdit.MinWidth = 300;
        DisposedReplacementLineEdit.OnTextChanged += (args) => { DisposedReplacementPrototypeValid = ValidateProto(args); confirmButton!.Disabled = !DispensingPrototypeValid || !DisposedReplacementPrototypeValid; };

        AutoCleanUpCheckBox =           AddOption<CheckBox>("AutoCleanUpCheckBox");

        
        DispensingPrototypeLineEdit.SetText(dispenserComp.DispensingPrototype);
        AutoDisposeCheckBox.Pressed = dispenserComp.AutoDispose;
        CanManuallyDisposeCheckBox.Pressed = dispenserComp.CanManuallyDispose;
        InfiniteCheckBox.Pressed = dispenserComp.Infinite;
        LimitLineEdit.SetText(dispenserComp.Limit.ToString());
        ReplaceDisposedItemsCheckBox.Pressed = dispenserComp.ReplaceDisposedItems;
        DisposedReplacementLineEdit.SetText(dispenserComp.DisposedReplacement);
        AutoCleanUpCheckBox.Pressed = dispenserComp.AutoCleanUp;

        window.OpenCentered();
    }


    private T AddOption<T>(string text) where T : Control, IDisposable, new()
    {
        var box = this.CreateDisposableControl<BoxContainer>();
        box.HorizontalAlignment = Control.HAlignment.Stretch;
        var label = this.CreateDisposableControl<Label>();
        label.Text = text;
        label.HorizontalExpand = true;
        label.HorizontalAlignment = Label.HAlignment.Left;
        box.AddChild(label);
        var control = this.CreateDisposableControl<T>();
        control.HorizontalAlignment = Control.HAlignment.Right;
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

    private void TrySubmit()
    {
        if (confirmButton!.Disabled)
        {
            var msg = new EventItemDispenserNewConfigBoundUserInterfaceMessage()
            {
                DispensingPrototype = DispensingPrototypeLineEdit!.Text,
                AutoDispose = AutoDisposeCheckBox!.Pressed,
                CanManuallyDispose = CanManuallyDisposeCheckBox!.Pressed,
                Infinite = InfiniteCheckBox!.Pressed,
                Limit = int.Parse(LimitLineEdit!.Text),
                ReplaceDisposedItems = ReplaceDisposedItemsCheckBox!.Pressed,
                DisposedReplacement = DisposedReplacementLineEdit!.Text,
                AutoCleanUp = AutoCleanUpCheckBox!.Pressed
            };
            SendMessage(msg);
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if(message is EventItemDispenserNewProtoBoundUserInterfaceMessage { } msg)
        {
            DispensingPrototypeLineEdit!.SetText(msg.DispensingPrototype);
        }
    }
}
