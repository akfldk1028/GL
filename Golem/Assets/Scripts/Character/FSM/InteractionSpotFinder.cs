using UnityEngine;

namespace Golem.Character.FSM
{
    /// <summary>
    /// Utility for finding interaction objects by tag.
    /// </summary>
    public static class InteractionSpotFinder
    {
        public static Transform FindByTag(string tag, int index = 0)
        {
            GameObject[] objs = null;
            try { objs = GameObject.FindGameObjectsWithTag(tag); } catch { return null; }
            if (objs == null || objs.Length == 0) return null;
            int idx = Mathf.Clamp(index, 0, objs.Length - 1);
            return objs[idx].transform;
        }

        public static Transform FindChair(int number)
            => FindByTag("Caffee Chair", number - 1);

        public static Transform FindArcade()
            => FindByTag("Arcade");

        public static Transform FindClawMachine()
            => FindByTag("Claw Machine");

        public static Transform FindSlotMachineChair()
            => FindByTag("Slot Machine Chair");

        public static Transform FindAdDisplay()
            => FindByTag("Cafe Ad Display");

        public static Transform FindByNameContains(string namePart)
        {
            if (string.IsNullOrEmpty(namePart)) return null;
            var all = Object.FindObjectsOfType<Transform>();
            namePart = namePart.ToLower();
            foreach (var t in all)
                if (t != null && t.name != null && t.name.ToLower().Contains(namePart))
                    return t;
            return null;
        }
    }
}
