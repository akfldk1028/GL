using Golem.Character.FSM;
using UnityEngine;
using UnityEngine.AI;

namespace Golem.Character.Modules
{
    /// <summary>
    /// Shared context for all behavior modules.
    /// Set once during initialization, references stay stable.
    /// </summary>
    public class BehaviorModuleContext
    {
        public Animator Animator { get; set; }
        public NavMeshAgent NavAgent { get; set; }
        public Transform CharacterTransform { get; set; }
        public CharacterBehaviorFSM FSM { get; set; }

        // Bone references for procedural animation
        public Transform HeadBone { get; set; }
        public Transform SpineBone { get; set; }
        public Transform HipBone { get; set; }

        // Tuning config
        public BehaviorConfigSO Config { get; set; }
    }
}
