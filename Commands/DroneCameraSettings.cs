using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Sliders for the FPV camera mount and lens. Uptilt is deliberately absent - DroneTilt
    /// owns it (and carries its own slider), so there is still exactly one place that writes
    /// DroneState.CameraUpTilt rather than two that can disagree about the persisted value.
    ///
    /// Mount offset is read every LateUpdate and FOV is re-stamped there too, so all three
    /// sliders retune a drone that is already flying.
    /// </summary>
    internal class DroneCameraSettings : CustomCommand
    {
        public override string Name => "Drone Camera";
        public override string Description => "Where the FPV lens sits on the frame and how wide it sees. Applies mid-flight.";
        public override string Format => "/dronecamsettings";
        public override string Category => "Drone";

        public override bool HasConfig => true;
        public override bool PersistConfig => true;

        public DroneCameraSettings()
        {
            // Range spans from behind the frame (negative - chase-cam-ish, you see the whole
            // airframe) to out past the nose, where the quad disappears behind the lens.
            AddSlider(new CommandSlider(
                "Forward Offset", -0.5f, 0.5f, DroneState.CameraForwardMargin, 0.01f, false,
                "Metres forward of the frame centre the lens sits",
                value => DroneState.CameraForwardMargin = value));

            AddSlider(new CommandSlider(
                "Height Offset", -0.2f, 0.4f, DroneState.CameraHeightOffset, 0.005f, false,
                "Metres above the frame centre the lens sits",
                value => DroneState.CameraHeightOffset = value));

            // 150 is past what any real FPV lens does, but the fisheye rush is half the appeal.
            AddSlider(new CommandSlider(
                "Field of View", 50f, 150f, DroneState.CameraFov, 1f, false,
                "Lens FOV in degrees - wider is more fisheye",
                value => DroneState.CameraFov = value));
        }

        public override void LoadCustomConfig()
        {
            DroneState.CameraForwardMargin = GetSliderValue("Forward Offset", DroneState.CameraForwardMargin);
            DroneState.CameraHeightOffset = GetSliderValue("Height Offset", DroneState.CameraHeightOffset);
            DroneState.CameraFov = GetSliderValue("Field of View", DroneState.CameraFov);
        }

        public override void Execute(CommandInput message)
        {
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Camera fwd {DroneState.CameraForwardMargin:0.00}m, height {DroneState.CameraHeightOffset:0.00}m, " +
                $"FOV {DroneState.CameraFov:0}deg");
        }
    }
}
