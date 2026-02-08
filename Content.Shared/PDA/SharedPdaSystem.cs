using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.PDA
{
    public abstract class SharedPdaSystem : EntitySystem
    {
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
        [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

        [Dependency] private readonly IGameTiming _gameTiming = default!; // WD EDIT

        // WD EDIT START
        /// <summary>
        ///     A set of pda that are currently opening, closing, or just queued to open/close after some delay.
        /// </summary>
        private readonly HashSet<Entity<PdaComponent>> _activePda = new();
        // WD EDIT END

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PdaComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PdaComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<PdaComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
            SubscribeLocalEvent<PdaComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

            SubscribeLocalEvent<PdaComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);

            // WD EDIT START
            SubscribeLocalEvent<PdaComponent, AfterAutoHandleStateEvent>(OnHandleState);

            SubscribeLocalEvent<PdaComponent, BoundUIClosedEvent>(OnUIClose);
            SubscribeLocalEvent<PdaComponent, BoundUIOpenedEvent>(OnUIOpen);
            // WD EDIT END
        }
        protected virtual void OnComponentInit(EntityUid uid, PdaComponent pda, ComponentInit args)
        {
            if (pda.IdCard != null)
                pda.IdSlot.StartingItem = pda.IdCard;

            // WD EDIT START
            if (pda.NextStateChange != null)
                _activePda.Add((uid, pda));
            // WD EDIT END

            ItemSlotsSystem.AddItemSlot(uid, PdaComponent.PdaIdSlotId, pda.IdSlot);
            ItemSlotsSystem.AddItemSlot(uid, PdaComponent.PdaPenSlotId, pda.PenSlot);
            ItemSlotsSystem.AddItemSlot(uid, PdaComponent.PdaPaiSlotId, pda.PaiSlot);
            ItemSlotsSystem.AddItemSlot(uid, PdaComponent.PdaPassportSlotId, pda.PassportSlot);

            UpdatePdaAppearance(uid, pda);
        }

        private void OnComponentRemove(EntityUid uid, PdaComponent pda, ComponentRemove args)
        {
            ItemSlotsSystem.RemoveItemSlot(uid, pda.IdSlot);
            ItemSlotsSystem.RemoveItemSlot(uid, pda.PenSlot);
            ItemSlotsSystem.RemoveItemSlot(uid, pda.PaiSlot);
            ItemSlotsSystem.RemoveItemSlot(uid, pda.PassportSlot);

            _activePda.Remove((uid, pda)); // WD EDIT
        }

        protected virtual void OnItemInserted(EntityUid uid, PdaComponent pda, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID == PdaComponent.PdaIdSlotId)
                pda.ContainedId = args.Entity;

            UpdatePdaAppearance(uid, pda);
        }

        protected virtual void OnItemRemoved(EntityUid uid, PdaComponent pda, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID == pda.IdSlot.ID)
                pda.ContainedId = null;

            UpdatePdaAppearance(uid, pda);
        }

        private void OnGetAdditionalAccess(EntityUid uid, PdaComponent component, ref GetAdditionalAccessEvent args)
        {
            if (component.ContainedId is { } id)
                args.Entities.Add(id);
        }

        // WD EDIT START
        private void OnHandleState(Entity<PdaComponent> ent, ref AfterAutoHandleStateEvent args)
        {
            if (ent.Comp.NextStateChange == null)
                _activePda.Remove(ent);
            else
                _activePda.Add(ent);
        }

        private void OnUIClose(Entity<PdaComponent> ent, ref BoundUIClosedEvent args)
        {
            if (!PdaUiKey.Key.Equals(args.UiKey))
                return;

            SetState(ent.AsNullable(), PdaState.Closing);
        }

        protected virtual void OnUIOpen(Entity<PdaComponent> ent, ref BoundUIOpenedEvent args)
        {
            if (!PdaUiKey.Key.Equals(args.UiKey))
                return;

            SetState(ent.AsNullable(), PdaState.Opening);
        }
        // WD EDIT END

        private void UpdatePdaAppearance(EntityUid uid, PdaComponent pda)
        {
            Appearance.SetData(uid, PdaVisuals.IdCardInserted, pda.ContainedId != null);
        }

        // WD EDIT START
        public override void Update(float frameTime)
        {
            foreach (var ent in _activePda.ToList())
            {
                if (ent.Comp.Deleted || ent.Comp.NextStateChange == null)
                {
                    _activePda.Remove(ent);
                    continue;
                }

                if (Paused(ent) || ent.Comp.NextStateChange.Value > _gameTiming.CurTime)
                    continue;

                switch (ent.Comp.State)
                {
                    case PdaState.Closing:
                        SetState(ent.AsNullable(), PdaState.Closed);
                        break;
                    case PdaState.Opening:
                        SetState(ent.AsNullable(), PdaState.Open);
                        break;
                    case PdaState.Open:
                    case PdaState.Closed:
                        _activePda.Remove((ent, ent.Comp));
                        break;
                }
            }
        }

        private void SetState(Entity<PdaComponent?> ent, PdaState state)
        {
            if (!Resolve(ent, ref ent.Comp) || state == ent.Comp.State)
                return;

            switch (state)
            {
                case PdaState.Opening:
                    _activePda.Add((ent, ent.Comp));
                    ent.Comp.NextStateChange = _gameTiming.CurTime + ent.Comp.OpeningAnimationTime;
                    break;
                case PdaState.Closing:
                    _activePda.Add((ent, ent.Comp));
                    ent.Comp.NextStateChange = _gameTiming.CurTime + ent.Comp.ClosingAnimationTime;
                    break;
                case PdaState.Open:
                case PdaState.Closed:
                    _activePda.Remove((ent, ent.Comp));
                    break;
            }

            ent.Comp.State = state;
            Dirty(ent);

            Appearance.SetData(ent, PdaVisuals.State, ent.Comp.State);
        }
        // WD EDIT END
    }
}
