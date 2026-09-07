using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Turns an item into a flying drone. Shared by the /fpvdrone command (which spawns a fresh
    /// payload first) and by throwing a drone bought from a shop stand (which converts the item
    /// already in your hands), so both routes produce exactly the same craft.
    /// </summary>
    internal static class DroneLauncher
    {
        /// <summary>Converts an item the player is already holding into a drone and launches it.</summary>
        public static bool LaunchExisting(Item item, Player pilot)
        {
            if (!item || !pilot)
            {
                return false;
            }
            if (DroneState.DroneInFlight)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("A drone is already in flight.");
                return false;
            }

            Camera cam = GameInfo.CurCamera;
            Vector3 forward = (bool)cam ? cam.transform.forward : pilot.Transform.forward;

            KamikazeDrone.Launch(item, pilot, forward);
            DroneState.DroneInFlight = true;
            return true;
        }

        /// <summary>
        /// Spawns a fresh payload item in front of the player and launches it as a drone.
        ///
        /// Works as a client as well as the host: only the host may Instantiate+Spawn directly,
        /// so a client goes through Server.BuyItem instead (a ServerRpc, with isFree set), and
        /// the drone is launched when the resulting item lands in the player's hands.
        /// </summary>
        public static bool LaunchNew(Item payload, Player pilot)
        {
            if (!payload || !pilot)
            {
                return false;
            }
            if (DroneState.DroneInFlight)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("A drone is already in flight.");
                return false;
            }

            Camera cam = GameInfo.CurCamera;
            if (!cam)
            {
                return false;
            }

            bool isHost = (bool)Server.Instance && Server.Instance.IsServerInitialized;
            if (isHost)
            {
                Vector3 spawnPos = cam.transform.position + cam.transform.forward * 1.5f;
                Item item = Object.Instantiate(payload, spawnPos, Quaternion.LookRotation(cam.transform.forward));
                Server.Instance.Spawn(item.gameObject);

                KamikazeDrone.Launch(item, pilot, cam.transform.forward);
                DroneState.DroneInFlight = true;
                return true;
            }

            if (!Server.Instance)
            {
                return false;
            }

            // Client: ask the server for the item, then launch it once it arrives. Requesting it
            // free keeps /fpvdrone behaving as the debug shortcut it is, rather than charging.
            pilot.Hands.PrepareForItemPickup(payload.ID);
            Server.Instance.BuyItem(
                payload.ID,
                pilot,
                pilot.Holding.HeldItem,
                cam.transform.position + cam.transform.forward * 1.5f,
                Quaternion.LookRotation(cam.transform.forward),
                isFree: true);

            DroneShop.RequestLaunchOnArrival(payload.ID);
            return true;
        }
    }
}
