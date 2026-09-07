using CTDynamicModMenu.Commands;
using HTFDrone.Drone;
using UnityEngine;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Sets how far the FPV camera is angled up from the airframe, in degrees. Real quads are
    /// flown nose-down to go fast, so the cam is shimmed up to put the horizon back in frame -
    /// more tilt means more speed before you're staring at the ground.
    ///
    /// Because the Format carries an argument, the mod menu renders this as a button that opens
    /// an input popup, so it's settable by clicking as well as by typing - the menu has no
    /// numeric field of its own (a command is either a toggle or a plain button).
    /// </summary>
    internal class DroneTilt : CustomCommand
    {
        public override string Name => "Drone Camera Uptilt";
        public override string Description => "Sets how many degrees the FPV camera is angled up from the airframe (0-90).";
        public override string Format => "/dronetilt <degrees>";
        public override string Category => "Drone";

        public override bool HasConfig => true;
        public override bool PersistConfig => true;

        private const string ConfigKey = "Drone Camera Uptilt: Degrees";

        public override void Execute(CommandInput message)
        {
            if (message == null || message.Args.Count < 1 || !float.TryParse(message.Args[0], out float degrees))
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                    $"Usage: {Format}  (current: {DroneState.CameraUpTilt:0}deg)");
                return;
            }

            DroneState.CameraUpTilt = Mathf.Clamp(degrees, DroneState.MinCameraUpTilt, DroneState.MaxCameraUpTilt);

            // The menu's argument popup calls Execute directly and never calls SaveConfig, so the
            // save has to happen here or a value set by clicking would be forgotten on restart
            // while one typed into the command window survived.
            SaveConfig();

            // Read every LateUpdate, so this lands on a drone that's already airborne.
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"FPV camera uptilt -> {DroneState.CameraUpTilt:0}deg");
        }

        public override void LoadCustomConfig()
        {
            DroneState.CameraUpTilt = Mathf.Clamp(
                CTDynamicModMenu.CTDynamicModMenu.Instance.Config.Bind(
                    "Command Settings", ConfigKey, DroneState.CameraUpTilt,
                    "Degrees the FPV camera is angled up from the drone's airframe").Value,
                DroneState.MinCameraUpTilt,
                DroneState.MaxCameraUpTilt);
        }

        public override void SaveCustomConfig()
        {
            CTDynamicModMenu.CTDynamicModMenu.Instance.Config.Bind(
                "Command Settings", ConfigKey, DroneState.CameraUpTilt,
                "Degrees the FPV camera is angled up from the drone's airframe").Value = DroneState.CameraUpTilt;
        }
    }
}
