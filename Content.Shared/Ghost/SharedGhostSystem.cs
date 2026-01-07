using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.InteractionVerbs.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost
{
    /// <summary>
    /// System for the <see cref="GhostComponent"/>.
    /// Prevents ghosts from interacting when <see cref="GhostComponent.CanGhostInteract"/> is false.
    /// </summary>
    public abstract class SharedGhostSystem : EntitySystem
    {
        [Dependency] protected readonly SharedPopupSystem Popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GhostComponent, UseAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<GhostComponent, InteractionAttemptEvent>(OnAttemptInteract);
            SubscribeLocalEvent<GhostComponent, EmoteAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<GhostComponent, DropAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<GhostComponent, PickupAttemptEvent>(OnAttempt);
            SubscribeLocalEvent<GhostComponent, InteractionVerbAttemptEvent>(OnAttempt);
        }

        private void OnAttemptInteract(Entity<GhostComponent> ent, ref InteractionAttemptEvent args)
        {
            if (!ent.Comp.CanGhostInteract)
                args.Cancelled = true;
        }

        private void OnAttempt(EntityUid uid, GhostComponent component, CancellableEntityEventArgs args)
        {
            if (!component.CanGhostInteract)
                args.Cancel();
        }

        public void SetTimeOfDeath(EntityUid uid, TimeSpan value, GhostComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            component.TimeOfDeath = value;
        }

        public void SetCanReturnToBody(EntityUid uid, bool value, GhostComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.CanReturnToBody = value;
        }

        public void SetCanReturnToBody(GhostComponent component, bool value)
        {
            component.CanReturnToBody = value;
        }
    }

    /// <summary>
    /// A client to server request to get places a ghost can warp to.
    /// Response is sent via <see cref="GhostWarpsResponseEvent"/>
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpsRequestEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Goobstation - A server to client request for them to spawn at the ghost bar
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostBarSpawnEvent : EntityEventArgs
    {
    }

     // WWDP-Start
     /// <summary>
     /// An player body a ghost can warp to.
     /// This is used as part of <see cref="GhostWarpsResponseEvent"/>
     /// </summary>
     [Serializable, NetSerializable]
     public struct GhostWarpPlayer
     {
         public GhostWarpPlayer(NetEntity entity, string playerName, string playerJobName, string playerDepartmentID, bool isGhost, bool isLeft, bool isDead, bool isAlive)
         {
             Entity = entity;
             Name = playerName;
             JobName = playerJobName;
             DepartmentID = playerDepartmentID;

             Status =
                 (isGhost ? PlayerStatus.IsGhost : 0) |
                 (isAlive ? PlayerStatus.IsAlive : 0) |
                 (isDead ? PlayerStatus.IsDead : 0) |
                 (isLeft ? PlayerStatus.IsLeft : 0) ;
         }

         /// <summary>
         /// The entity representing the warp point.
         /// This is passed back to the server in <see cref="GhostWarpToTargetRequestEvent"/>
         /// </summary>
         public NetEntity Entity { get; }

         /// <summary>
         /// The display player name to be surfaced in the ghost warps menu
         /// </summary>
         public string Name { get; }

         /// <summary>
         /// The display player job to be surfaced in the ghost warps menu
         /// </summary>

         public string JobName { get; }

         /// <summary>
         /// The display player department to be surfaced in the ghost warps menu
         /// </summary>
         public string DepartmentID { get; set; }

         public PlayerStatus Status { get; }
     }

     [Serializable, NetSerializable, Flags]
     public enum PlayerStatus : byte
     {
        None = 0,
        IsGhost = 1,
        IsAlive = 1 << 1,
        IsDead = 1 << 2,
        IsLeft = 1 << 3,
     }

     [Serializable, NetSerializable]
     public struct GhostWarpGlobalRoles
     {
         public GhostWarpGlobalRoles(NetEntity entity, string playerName, string roleDescription, string prototypeID, bool isDead)
         {
             Entity = entity;
             Name = playerName;
             RoleDescription = roleDescription;
             PrototypeID = prototypeID;
             IsDead = isDead;
         }

         /// <summary>
         /// The entity representing the warp point.
         /// This is passed back to the server in <see cref="GhostWarpToTargetRequestEvent"/>
         /// </summary>
         public NetEntity Entity { get; }

         /// <summary>
         /// The display player name to be surfaced in the ghost warps menu
         /// </summary>
         public string Name { get; }

         /// <summary>
         /// The display antagonist description to be surfaced in the ghost warps menu
         /// </summary>
         public string RoleDescription { get; }

         /// <summary>
         /// A antagonist prototype id
         /// </summary>
         public string PrototypeID { get; }

         /// <summary>
         /// Is antagonist dead
         /// </summary>
         public bool IsDead { get;  }

     }
    // WWDP-End

    /// <summary>
    /// An individual place a ghost can warp to.
    /// This is used as part of <see cref="GhostWarpsResponseEvent"/>
    /// </summary>
    [Serializable, NetSerializable]
    public struct GhostWarpPlace // WWDP-Edit: GhostWarp > GhostWarpPlace
    {
        // WWDP-Edit-Start
        public GhostWarpPlace(NetEntity entity, string name, string description)
        {
            Entity = entity;
            Name = name;
            Description = description;
        }
        // WWDP-Edit-End

        /// <summary>
        /// The entity representing the warp point.
        /// This is passed back to the server in <see cref="GhostWarpToTargetRequestEvent"/>
        /// </summary>
        public NetEntity Entity { get; }

        /// <summary>
        /// The display name to be surfaced in the ghost warps menu
        /// </summary>
        public string Name { get; } // WWDP-Edit: DisplayName > Name

        // WD EDIT START
        /// <summary>
        /// Display name to be surfaced in the ghost warps menu
        /// </summary>
        public string Description { get;  } 
        // WD EDIT END
    }

    /// <summary>
    /// A server to client response for a <see cref="GhostWarpsRequestEvent"/>.
    /// Contains players, and locations a ghost can warp to
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpsResponseEvent : EntityEventArgs
    {
        // WWDP-Start
        public GhostWarpsResponseEvent(List<GhostWarpPlayer> players, List<GhostWarpPlace> places, List<GhostWarpGlobalRoles> roles)
        {
            Players = players;
            Places = places;
            Roles = roles;
        }

        /// <summary>
        /// A list of players to teleport.
        /// </summary>
        public List<GhostWarpPlayer> Players { get; }

        /// <summary>
        /// A list of warp points.
        /// </summary>
        public List<GhostWarpPlace> Places { get; }

        /// <summary>
        /// A list of antagonists to teleport.
        /// </summary>
        public List<GhostWarpGlobalRoles> Roles { get; }
        // WWDP-End
    }

    /// <summary>
    ///  A client to server request for their ghost to be warped to an entity
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostWarpToTargetRequestEvent : EntityEventArgs
    {
        public NetEntity Target { get; }

        public GhostWarpToTargetRequestEvent(NetEntity target)
        {
            Target = target;
        }
    }

    /// <summary>
    /// A client to server request for their ghost to be warped to the most followed entity.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostnadoRequestEvent : EntityEventArgs;

    /// <summary>
    /// A client to server request for their ghost to return to body
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostReturnToBodyRequest : EntityEventArgs
    {
    }

    /// <summary>
    /// A server to client update with the available ghost role count
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GhostUpdateGhostRoleCountEvent : EntityEventArgs
    {
        public int AvailableGhostRoles { get; }

        public GhostUpdateGhostRoleCountEvent(int availableGhostRoleCount)
        {
            AvailableGhostRoles = availableGhostRoleCount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GhostReturnToRoundRequest : EntityEventArgs;
}
