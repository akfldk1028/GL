using UnityEngine;
using UnityEngine.AI;

namespace Golem.Character.FSM
{
    /// <summary>
    /// Blackboard shared across all character FSM states.
    /// Set by GolemCharacterController and CharacterCommandRouter before transitions.
    /// </summary>
    public class CharacterStateContext
    {
        // Core references
        public PointClickController PointClick { get; set; }
        public Animator Animator { get; set; }
        public NavMeshAgent NavAgent { get; set; }
        public Transform CharacterTransform { get; set; }
        public CharacterBehaviorFSM FSM { get; set; }

        // Interaction data — set before transitioning to Arriving/SitTransition
        public Transform InteractionSpot { get; set; }
        public Collider DisabledCollider { get; set; }
        public Vector3 PendingDestination { get; set; }

        // Pending interaction target — where Arriving should transition to
        public CharacterStateId PendingInteractionState { get; set; }

        /// <summary>
        /// Clears interaction context. Called on Idle enter.
        /// </summary>
        public void ClearInteraction()
        {
            InteractionSpot = null;
            PendingInteractionState = CharacterStateId.None;
            PendingDestination = Vector3.zero;
            // DisabledCollider is NOT cleared here — re-enabled by the interaction state's Exit
        }

        /// <summary>
        /// Re-enables the previously disabled collider (chair, arcade, etc.)
        /// </summary>
        public void RestoreDisabledCollider()
        {
            if (DisabledCollider != null)
            {
                DisabledCollider.enabled = true;
                DisabledCollider = null;
            }
        }
    }
}
