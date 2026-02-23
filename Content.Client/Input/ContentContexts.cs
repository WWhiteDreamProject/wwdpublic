using Content.Shared.Input;
using Robust.Shared.Input;

namespace Content.Client.Input
{
    /// <summary>
    ///     Contains a helper function for setting up all content
    ///     contexts, and modifying existing engine ones.
    /// </summary>
    public static class ContentContexts
    {
        public static void SetupContexts(IInputContextContainer contexts)
        {
            var common = contexts.GetContext("common");
            common.AddFunction(ContentKeyFunctions.FocusChat);
            common.AddFunction(ContentKeyFunctions.FocusLocalChat);
            common.AddFunction(ContentKeyFunctions.FocusEmote);
            common.AddFunction(ContentKeyFunctions.FocusWhisperChat);
            common.AddFunction(ContentKeyFunctions.FocusRadio);
            common.AddFunction(ContentKeyFunctions.FocusLOOC);
            common.AddFunction(ContentKeyFunctions.FocusOOC);
            common.AddFunction(ContentKeyFunctions.FocusAdminChat);
            common.AddFunction(ContentKeyFunctions.FocusConsoleChat);
            common.AddFunction(ContentKeyFunctions.FocusDeadChat);
            common.AddFunction(ContentKeyFunctions.CycleChatChannelForward);
            common.AddFunction(ContentKeyFunctions.CycleChatChannelBackward);
            common.AddFunction(ContentKeyFunctions.EscapeContext);
            common.AddFunction(ContentKeyFunctions.ExamineEntity);
            common.AddFunction(ContentKeyFunctions.OpenAHelp);
            common.AddFunction(ContentKeyFunctions.TakeScreenshot);
            common.AddFunction(ContentKeyFunctions.TakeScreenshotNoUI);
            common.AddFunction(ContentKeyFunctions.ToggleFullscreen);
            common.AddFunction(ContentKeyFunctions.MoveStoredItem);
            common.AddFunction(ContentKeyFunctions.RotateStoredItem);
            common.AddFunction(ContentKeyFunctions.SaveItemLocation);
            common.AddFunction(ContentKeyFunctions.Point);
            common.AddFunction(ContentKeyFunctions.ZoomOut);
            common.AddFunction(ContentKeyFunctions.ZoomIn);
            common.AddFunction(ContentKeyFunctions.ResetZoom);
            common.AddFunction(ContentKeyFunctions.InspectEntity);
            common.AddFunction(ContentKeyFunctions.ToggleRoundEndSummaryWindow);

            // Not in engine, because engine cannot check for sanbox/admin status before starting placement.
            common.AddFunction(ContentKeyFunctions.EditorCopyObject);

            // Not in engine because the engine doesn't understand what a flipped object is
            common.AddFunction(ContentKeyFunctions.EditorFlipObject);

            // Not in engine so that the RCD can rotate objects
            common.AddFunction(EngineKeyFunctions.EditorRotateObject);

            var human = contexts.GetContext("human");
            human.AddFunction(EngineKeyFunctions.MoveUp);
            human.AddFunction(EngineKeyFunctions.MoveDown);
            human.AddFunction(EngineKeyFunctions.MoveLeft);
            human.AddFunction(EngineKeyFunctions.MoveRight);
            human.AddFunction(EngineKeyFunctions.Walk);
            human.AddFunction(ContentKeyFunctions.SwapHands);
            human.AddFunction(ContentKeyFunctions.Drop);
            human.AddFunction(ContentKeyFunctions.UseItemInHand);
            human.AddFunction(ContentKeyFunctions.AltUseItemInHand);
            human.AddFunction(ContentKeyFunctions.OpenCharacterMenu);
            human.AddFunction(ContentKeyFunctions.OpenLanguageMenu);
            human.AddFunction(ContentKeyFunctions.ActivateItemInWorld);
            human.AddFunction(ContentKeyFunctions.ThrowItemInHand);
            human.AddFunction(ContentKeyFunctions.AltActivateItemInWorld);
            human.AddFunction(ContentKeyFunctions.TryPullObject);
            human.AddFunction(ContentKeyFunctions.MovePulledObject);
            human.AddFunction(ContentKeyFunctions.ReleasePulledObject);
            human.AddFunction(ContentKeyFunctions.OpenCraftingMenu);
            human.AddFunction(ContentKeyFunctions.OpenInventoryMenu);
            human.AddFunction(ContentKeyFunctions.SmartEquipBackpack);
            human.AddFunction(ContentKeyFunctions.SmartEquipBelt);
            human.AddFunction(ContentKeyFunctions.SmartEquipPocket1);
            human.AddFunction(ContentKeyFunctions.SmartEquipPocket2);
            human.AddFunction(ContentKeyFunctions.SmartEquipSuitStorage);
            human.AddFunction(ContentKeyFunctions.OpenBackpack);
            human.AddFunction(ContentKeyFunctions.OpenBelt);
            human.AddFunction(ContentKeyFunctions.OfferItem);
            human.AddFunction(ContentKeyFunctions.ToggleStanding);
            human.AddFunction(ContentKeyFunctions.ToggleCrawlingUnder);
            human.AddFunction(ContentKeyFunctions.MouseMiddle);
            human.AddFunction(ContentKeyFunctions.RotateObjectClockwise);
            human.AddFunction(ContentKeyFunctions.RotateObjectCounterclockwise);
            human.AddFunction(ContentKeyFunctions.FlipObject);
            human.AddFunction(ContentKeyFunctions.ArcadeUp);
            human.AddFunction(ContentKeyFunctions.ArcadeDown);
            human.AddFunction(ContentKeyFunctions.ArcadeLeft);
            human.AddFunction(ContentKeyFunctions.ArcadeRight);
            human.AddFunction(ContentKeyFunctions.Arcade1);
            human.AddFunction(ContentKeyFunctions.Arcade2);
            human.AddFunction(ContentKeyFunctions.Arcade3);
            // WD EDIT START
            human.AddFunction(ContentKeyFunctions.PreciseDrop);
            human.AddFunction(ContentKeyFunctions.MouseWheelUp);
            human.AddFunction(ContentKeyFunctions.MouseWheelDown);
            human.AddFunction(ContentKeyFunctions.OpenEmotesMenu);
            human.AddFunction(ContentKeyFunctions.ToggleCombatMode);
            human.AddFunction(ContentKeyFunctions.LookUp);
            human.AddFunction(ContentKeyFunctions.TargetDollHead);
            human.AddFunction(ContentKeyFunctions.TargetDollChest);
            human.AddFunction(ContentKeyFunctions.TargetDollGroin);
            human.AddFunction(ContentKeyFunctions.TargetDollRightArm);
            human.AddFunction(ContentKeyFunctions.TargetDollRightHand);
            human.AddFunction(ContentKeyFunctions.TargetDollLeftArm);
            human.AddFunction(ContentKeyFunctions.TargetDollLeftHand);
            human.AddFunction(ContentKeyFunctions.TargetDollRightLeg);
            human.AddFunction(ContentKeyFunctions.TargetDollRightFoot);
            human.AddFunction(ContentKeyFunctions.TargetDollLeftLeg);
            human.AddFunction(ContentKeyFunctions.TargetDollLeftFoot);
            human.AddFunction(ContentKeyFunctions.TargetDollTail);
            human.AddFunction(ContentKeyFunctions.TargetDollEyes);
            human.AddFunction(ContentKeyFunctions.TargetDollMouth);
            // WD EDIT END

            // actions should be common (for ghosts, mobs, etc)
            common.AddFunction(ContentKeyFunctions.OpenActionsMenu);

            foreach (var boundKey in ContentKeyFunctions.GetHotbarBoundKeys())
                common.AddFunction(boundKey);

            var aghost = contexts.New("aghost", "common");
            aghost.AddFunction(EngineKeyFunctions.MoveUp);
            aghost.AddFunction(EngineKeyFunctions.MoveDown);
            aghost.AddFunction(EngineKeyFunctions.MoveLeft);
            aghost.AddFunction(EngineKeyFunctions.MoveRight);
            aghost.AddFunction(EngineKeyFunctions.Walk);
            aghost.AddFunction(ContentKeyFunctions.SwapHands);
            aghost.AddFunction(ContentKeyFunctions.Drop);
            aghost.AddFunction(ContentKeyFunctions.UseItemInHand);
            aghost.AddFunction(ContentKeyFunctions.AltUseItemInHand);
            aghost.AddFunction(ContentKeyFunctions.ActivateItemInWorld);
            aghost.AddFunction(ContentKeyFunctions.ThrowItemInHand);
            aghost.AddFunction(ContentKeyFunctions.AltActivateItemInWorld);
            aghost.AddFunction(ContentKeyFunctions.TryPullObject);
            aghost.AddFunction(ContentKeyFunctions.MovePulledObject);
            aghost.AddFunction(ContentKeyFunctions.ReleasePulledObject);
            // WD EDIT START
            aghost.AddFunction(ContentKeyFunctions.PreciseDrop);
            aghost.AddFunction(ContentKeyFunctions.MouseWheelUp);
            aghost.AddFunction(ContentKeyFunctions.MouseWheelDown);
            aghost.AddFunction(ContentKeyFunctions.ToggleCombatMode);
            aghost.AddFunction(ContentKeyFunctions.TargetDollHead);
            aghost.AddFunction(ContentKeyFunctions.TargetDollChest);
            aghost.AddFunction(ContentKeyFunctions.TargetDollGroin);
            aghost.AddFunction(ContentKeyFunctions.TargetDollRightArm);
            aghost.AddFunction(ContentKeyFunctions.TargetDollRightHand);
            aghost.AddFunction(ContentKeyFunctions.TargetDollLeftArm);
            aghost.AddFunction(ContentKeyFunctions.TargetDollLeftHand);
            aghost.AddFunction(ContentKeyFunctions.TargetDollRightLeg);
            aghost.AddFunction(ContentKeyFunctions.TargetDollRightFoot);
            aghost.AddFunction(ContentKeyFunctions.TargetDollLeftLeg);
            aghost.AddFunction(ContentKeyFunctions.TargetDollLeftFoot);
            aghost.AddFunction(ContentKeyFunctions.TargetDollTail);
            aghost.AddFunction(ContentKeyFunctions.TargetDollEyes);
            aghost.AddFunction(ContentKeyFunctions.TargetDollMouth);
            // WD EDIT END

            var ghost = contexts.New("ghost", "human");
            ghost.AddFunction(EngineKeyFunctions.MoveUp);
            ghost.AddFunction(EngineKeyFunctions.MoveDown);
            ghost.AddFunction(EngineKeyFunctions.MoveLeft);
            ghost.AddFunction(EngineKeyFunctions.MoveRight);
            ghost.AddFunction(EngineKeyFunctions.Walk);

            common.AddFunction(ContentKeyFunctions.OpenEntitySpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenSandboxWindow);
            common.AddFunction(ContentKeyFunctions.OpenTileSpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenDecalSpawnWindow);
            common.AddFunction(ContentKeyFunctions.OpenAdminMenu);
            common.AddFunction(ContentKeyFunctions.OpenGuidebook);
        }
    }
}
