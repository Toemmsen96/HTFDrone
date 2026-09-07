using CTDynamicModMenu.Commands;
using HTFDrone.Drone;
using UnityEngine;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Launches an FPV kamikaze drone: spawns one of the game's own Explosive items (dynamite by
    /// default) where the player is looking and flies it forward, hand-flown from there.
    /// Detonates on impact. Works as host or client; other players see the drone fly whether or
    /// not they have the mod installed.
    /// </summary>
    internal class StartDrone : CustomCommand
    {
        public override string Name => "FPV Kamikaze Drone";

        public override string Description => "Launches an explosive FPV drone you fly by hand. Optional arg: payload item name (default dynamite).";
        public override string Format => "/fpvdrone";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            // Works as a client too - DroneLauncher goes through the server for the item when we
            // aren't the host - but we still need to actually be in a game.
            if (!Server.Instance)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Not in a game.");
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

            Item payload = DronePayload.Resolve(payloadName);
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

            if (DroneLauncher.LaunchNew(payload, pilot))
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Drone away!");
            }
        }

    }
}
