using System.Collections.Generic;
using UnityEngine;

namespace Golem.Character.Autonomous
{
    public class SkillLibrary
    {
        private readonly List<SkillEntry> _skills = new List<SkillEntry>();
        private readonly MemoryConfigSO _config;

        public List<SkillEntry> Skills => _skills;

        public SkillLibrary(MemoryConfigSO config)
        {
            _config = config;
        }

        public void LoadFrom(List<SkillEntry> saved)
        {
            _skills.Clear();
            if (saved != null)
                _skills.AddRange(saved);
        }

        public SkillEntry Match(string situationPattern)
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].situationPattern == situationPattern)
                    return _skills[i];
            }
            return null;
        }

        public bool ShouldUseSkill(SkillEntry skill)
        {
            if (skill == null) return false;
            if (skill.useCount < _config.minSkillUses) return false;
            if (skill.SuccessRate < _config.skillConfidenceThreshold) return false;
            // Exploration: random chance to skip cached skill
            if (Random.value < _config.explorationRate) return false;
            return true;
        }

        public void RecordOutcome(string situationPattern, int actionId, string actionName, string target, bool succeeded)
        {
            var existing = Match(situationPattern);
            if (existing != null)
            {
                if (existing.recommendedActionId == actionId)
                {
                    // Same action — update counts
                    existing.useCount++;
                    if (succeeded)
                        existing.successCount++;
                }
                else
                {
                    // Different action for same situation — track as use
                    existing.useCount++;
                    if (succeeded && existing.SuccessRate < _config.skillReplacementThreshold)
                    {
                        // Replace with better-performing action
                        existing.recommendedActionId = actionId;
                        existing.actionName = actionName;
                        existing.target = target;
                        existing.successCount++;
                    }
                }
                return;
            }

            // Only create new skill entries from successes
            if (!succeeded) return;

            var entry = new SkillEntry
            {
                situationPattern = situationPattern,
                recommendedActionId = actionId,
                actionName = actionName,
                target = target,
                useCount = 1,
                successCount = 1
            };

            _skills.Add(entry);

            // Evict lowest success rate if over capacity
            while (_skills.Count > _config.maxSkills)
            {
                int worstIdx = 0;
                float worstRate = _skills[0].SuccessRate;
                for (int i = 1; i < _skills.Count; i++)
                {
                    if (_skills[i].SuccessRate < worstRate)
                    {
                        worstRate = _skills[i].SuccessRate;
                        worstIdx = i;
                    }
                }
                _skills.RemoveAt(worstIdx);
            }
        }

        public void Prune()
        {
            _skills.RemoveAll(s =>
                s.useCount >= _config.minSkillUses &&
                s.SuccessRate < _config.skillPruneThreshold);
        }
    }
}
