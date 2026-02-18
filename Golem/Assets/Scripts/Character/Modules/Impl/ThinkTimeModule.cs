using Golem.Character.FSM;
using UnityEngine;

namespace Golem.Character.Modules.Impl
{
    public class ThinkTimeModule : BaseBehaviorModule
    {
        public override string ModuleId => "thinkTime";

        public float GetPauseDuration(CharacterStateId from, CharacterStateId to)
        {
            if (Context?.Config == null) return 0f;

            if (from == CharacterStateId.Idle && to == CharacterStateId.Walking)
            {
                float min = Context.Config.thinkTimeMin;
                float max = Context.Config.thinkTimeMax;
                if (max <= 0f) return 0f;
                return Random.Range(min, max);
            }

            return 0f;
        }
    }
}
