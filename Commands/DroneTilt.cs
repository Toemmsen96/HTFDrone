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
    /// Settable three ways, all writing the same field: the slider below, the mod menu's
    /// argument popup (the Format carries an argument, so the menu renders a button that opens
    /// one), or /dronetilt typed directly. Execute() pushes the typed/popup value back into the
    /// slider so the menu never shows a stale number next to a value set from the console.
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
        private const string TiltSlider = "Uptilt";

        public DroneTilt()
        {
            AddSlider(new CommandSlider(
                TiltSlider,
                DroneState.MinCameraUpTilt,
                DroneState.MaxCameraUpTilt,
                DroneState.CameraUpTilt,
                1f,
                false,
                "Degrees the FPV camera is angled up from the airframe",
                value => DroneState.CameraUpTilt = value));
        }

        public override void Execute(CommandInput message)
        {
            if (message == null || message.Args.Count < 1 || !float.TryParse(message.Args[0], out float degrees))
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                    $"Usage: {Format}  (current: {DroneState.CameraUpTilt:0}deg)");
                return;
            }

            DroneState.CameraUpTilt = Mathf.Clamp(degrees, DroneState.MinCameraUpTilt, DroneState.MaxCameraUpTilt);

            // Keep the slider in step with a value typed in or set from the popup, otherwise the
            // menu would show the old position until something else moved it. Silent, because
            // OnValueChanged would only write back the value we just set.
            GetSlider(TiltSlider)?.SetValueSilent(DroneState.CameraUpTilt);

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
            // Two keys hold this now: the original ConfigKey and the base class's slider key.
            // The base class restores sliders before calling this, and it does so silently, so
            // the slider's value is the live one - read it back as the default for ConfigKey and
            // the two agree instead of the older key stomping a freshly dragged slider.
            float restored = GetSliderValue(TiltSlider, DroneState.CameraUpTilt);

            DroneState.CameraUpTilt = Mathf.Clamp(
                CTDynamicModMenu.CTDynamicModMenu.Instance.Config.Bind(
                    "Command Settings", ConfigKey, restored,
                    "Degrees the FPV camera is angled up from the drone's airframe").Value,
                DroneState.MinCameraUpTilt,
                DroneState.MaxCameraUpTilt);

            GetSlider(TiltSlider)?.SetValueSilent(DroneState.CameraUpTilt);
        }

        public override void SaveCustomConfig()
        {
            CTDynamicModMenu.CTDynamicModMenu.Instance.Config.Bind(
                "Command Settings", ConfigKey, DroneState.CameraUpTilt,
                "Degrees the FPV camera is angled up from the drone's airframe").Value = DroneState.CameraUpTilt;
        }
    }
}
