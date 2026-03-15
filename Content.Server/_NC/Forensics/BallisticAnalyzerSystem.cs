using Content.Shared._NC.Forensics;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Paper;
using Content.Shared.Chat;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Content.Server.Chat.Managers;
using System;
using Robust.Shared.Localization;

namespace Content.Server._NC.Forensics;

public sealed class BallisticAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly Content.Shared.Paper.PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BallisticAnalyzerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<BallisticAnalyzerComponent, BallisticAnalyzerStartMessage>(OnStartAnalysis);
        SubscribeLocalEvent<BallisticAnalyzerComponent, BallisticAnalysisDoAfterEvent>(OnAnalysisComplete);
        
        SubscribeLocalEvent<BallisticAnalyzerComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<BallisticAnalyzerComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
    }

    private void OnUiOpened(EntityUid uid, BallisticAnalyzerComponent component, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid, component);
    }

    private void OnItemInserted(EntityUid uid, BallisticAnalyzerComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateUiState(uid, component);
    }

    private void OnItemRemoved(EntityUid uid, BallisticAnalyzerComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateUiState(uid, component);
    }

    private void OnStartAnalysis(EntityUid uid, BallisticAnalyzerComponent component, BallisticAnalyzerStartMessage args)
    {
        // Проверяем наличие предметов в обоих слотах
        if (!_itemSlots.TryGetSlot(uid, component.BulletSlotId, out var bulletSlot) || bulletSlot.Item == null)
            return;
        if (!_itemSlots.TryGetSlot(uid, component.WeaponSlotId, out var weaponSlot) || weaponSlot.Item == null)
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.Actor, 5f, new BallisticAnalysisDoAfterEvent(), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _audio.PlayPvs("/Audio/Effects/beep1.ogg", uid); 
            UpdateUiState(uid, component, true);
        }
    }

    private void OnAnalysisComplete(EntityUid uid, BallisticAnalyzerComponent component, BallisticAnalysisDoAfterEvent args)
    {
        if (args.Cancelled || args.User == EntityUid.Invalid)
        {
            UpdateUiState(uid, component);
            return;
        }

        string? bulletHash = null;
        string? weaponHash = null;

        if (_itemSlots.TryGetSlot(uid, component.BulletSlotId, out var bulletSlot) && bulletSlot.Item is { } bullet)
        {
            if (TryComp<ForensicsBulletComponent>(bullet, out var bulletComp))
                bulletHash = bulletComp.Hash;
        }

        if (_itemSlots.TryGetSlot(uid, component.WeaponSlotId, out var weaponSlot) && weaponSlot.Item is { } weapon)
        {
            if (TryComp<ForensicsWeaponHashComponent>(weapon, out var weaponComp))
                weaponHash = weaponComp.Hash;
        }

        var result = BallisticMatchResult.NoMatch;
        if (bulletHash != null && weaponHash != null && bulletHash == weaponHash)
        {
            result = BallisticMatchResult.Match;
        }

        UpdateUiState(uid, component, false, result);
        
        // Уведомление в чат
        var resText = result == BallisticMatchResult.Match 
            ? Loc.GetString("forensics-ballistic-match-100") 
            : Loc.GetString("forensics-ballistic-match-0");
        var resColor = result == BallisticMatchResult.Match ? Robust.Shared.Maths.Color.Green : Robust.Shared.Maths.Color.Red;

        if (_playerManager.TryGetSessionByEntity(args.User, out var session))
        {
            var msg = Loc.GetString("forensics-ballistic-analysis-complete", ("result", resText));
            _chatManager.ChatMessageToOne(ChatChannel.Local, msg, msg, uid, false, session.Channel, resColor);
        }

        // Печать отчета
        PrintReport(uid, bulletHash, weaponHash, result);
    }

    private void PrintReport(EntityUid uid, string? bHash, string? wHash, BallisticMatchResult result)
    {
        var report = Spawn("Paper", Transform(uid).Coordinates);
        var resText = result == BallisticMatchResult.Match 
            ? Loc.GetString("forensics-ballistic-match-100") 
            : Loc.GetString("forensics-ballistic-match-0");

        if (TryComp<PaperComponent>(report, out var paper))
        {
            var error = Loc.GetString("forensics-report-error");
            string content = Loc.GetString("forensics-report-header") + "\n" +
                             Loc.GetString("forensics-report-separator") + "\n" +
                             Loc.GetString("forensics-report-bullet-hash", ("hash", bHash ?? error)) + "\n" +
                             Loc.GetString("forensics-report-weapon-hash", ("hash", wHash ?? error)) + "\n" +
                             Loc.GetString("forensics-report-result", ("result", resText)) + "\n" +
                             Loc.GetString("forensics-report-separator") + "\n" +
                             Loc.GetString("forensics-report-footer");
            
            _paper.SetContent((report, paper), content);
            _metaData.SetEntityName(report, Loc.GetString("forensics-report-name", ("result", resText)));
        }
    }

    private void UpdateUiState(EntityUid uid, BallisticAnalyzerComponent component, bool isAnalyzing = false, BallisticMatchResult result = BallisticMatchResult.None)
    {
        string? bulletHash = null;
        string? weaponHash = null;

        if (_itemSlots.TryGetSlot(uid, component.BulletSlotId, out var bulletSlot) && bulletSlot.Item is { } bullet)
        {
            if (TryComp<ForensicsBulletComponent>(bullet, out var bulletComp))
                bulletHash = bulletComp.Hash;
        }

        if (_itemSlots.TryGetSlot(uid, component.WeaponSlotId, out var weaponSlot) && weaponSlot.Item is { } weapon)
        {
            if (TryComp<ForensicsWeaponHashComponent>(weapon, out var weaponComp))
                weaponHash = weaponComp.Hash;
        }

        var state = new BallisticAnalyzerBuiState(bulletHash, weaponHash, isAnalyzing, result);
        _ui.SetUiState(uid, BallisticAnalyzerUiKey.Key, state);
    }
}
