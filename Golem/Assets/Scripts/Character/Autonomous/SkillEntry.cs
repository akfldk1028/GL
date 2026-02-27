namespace Golem.Character.Autonomous
{
    [System.Serializable]
    public class SkillEntry
    {
        public string situationPattern;
        public int recommendedActionId;
        public string actionName;
        public string target;
        public int useCount;
        public int successCount;

        public float SuccessRate => useCount > 0 ? (float)successCount / useCount : 0f;
    }
}
