using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Finds the Item prefab the drone is built from. Shared by the /fpvdrone command and the
    /// shop stand so they can't disagree about what a drone is.
    /// </summary>
    internal static class DronePayload
    {
        public static Item Resolve()
        {
            return Resolve(DroneState.PayloadItemName);
        }

        public static Item Resolve(string payloadName)
        {
            // GameInfo's spawnable tables are populated in its Awake, so this is null in the main
            // menu and during scene transitions. Called from a sceneLoaded hook, that would throw
            // every time a non-island scene loads - hence the try/catch rather than a null check
            // (GameInfo exposes no way to ask whether it's ready).
            try
            {
                Item named = GameInfo.GetSpawnable(payloadName);
                if ((bool)named && HasExplosive(named))
                {
                    return named;
                }
                return FindAnyExplosive();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static Item FindAnyExplosive()
        {
            foreach (Item item in Resources.LoadAll<Item>("Items"))
            {
                if (HasExplosive(item))
                {
                    return item;
                }
            }
            return null;
        }

        // Item.Explosive (the _explosive field) is only assigned in Explosive.Awake(), which
        // never runs on a raw Resources-loaded prefab asset (Unity only calls Awake on instances
        // actually placed in a scene). So we can't rely on item.Explosive here - check for the
        // component directly instead.
        public static bool HasExplosive(Item item)
        {
            return (bool)item && item.GetComponent<Explosive>() != null;
        }
    }
}
