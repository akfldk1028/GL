using UnityEngine;

namespace Golem.Character.Autonomous
{
    [System.Serializable]
    public class EpisodeEntry
    {
        public long timestampTicks;
        public int actionId;
        public string actionName;
        public string target;
        public string thought;
        public float importance;
        public bool succeeded;
        public float posX;
        public float posY;
        public float posZ;
        public string contextHash;
        public string reasoning;

        public System.DateTime Timestamp => new System.DateTime(timestampTicks);
        public Vector3 Position => new Vector3(posX, posY, posZ);
    }
}
