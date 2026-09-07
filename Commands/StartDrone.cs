using CTDynamicModMenu.Commands;
using HTFDrone.Drone;
using UnityEngine;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Launches an FPV kamikaze drone: spawns one of the game's own Explosive items (dynamite by
    /// default) where the player is looking and flies it forward, homing in on whatever
    /// creature/player it's roughly aimed at, then detonates on arrival/impact. No dedicated
    /// drone model - the payload item is the "drone". Host only (server-authoritative spawn).
    /// </summary>
    internal class StartDrone : CustomCommand
    {
        public override string Name => "FPV Kamikaze Drone";

        public override string Description => "Launches an explosive kamikaze drone from your view that homes in and detonates. Optional arg: payload item name (default dynamite).";
        public override string Format => "/fpvdrone";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            if (!Server.Instance || !Server.Instance.IsServerInitialized)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Must be hosting to launch the drone.");
                return;
            }

            if (DroneState.DroneInFlight)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("A drone is already in flight.");
                return;
            }

            string payloadName = (message != null && message.Args.Count > 0)
                ? string.Join("", message.Args).Replace(" ", "").ToLowerInvariant()
                : DroneState.PayloadItemName;

            Item namedPayload = GameInfo.GetSpawnable(payloadName);
            Item payload = (namedPayload && HasExplosive(namedPayload)) ? namedPayload : FindAnyExplosive();
            if (!payload)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage($"No explosive item found called '{payloadName}'.");
                return;
            }

            Camera cam = GameInfo.CurCamera;
            Player pilot = Player.LocalPlayer;
            if (!cam || !pilot)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("No local player/camera to launch from.");
                return;
            }

            Vector3 spawnPos = cam.transform.position + cam.transform.forward * 1.5f;
            Item item = Object.Instantiate(payload, spawnPos, Quaternion.LookRotation(cam.transform.forward));
            Server.Instance.Spawn(item.gameObject);

            KamikazeDrone.Launch(item, pilot, cam.transform.forward);
            DroneState.DroneInFlight = true;

            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Drone away!");
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
        private static bool HasExplosive(Item item)
        {
            return item.GetComponent<Explosive>() != null;
        }
    }
}
