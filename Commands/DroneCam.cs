using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Tunes where the FPV camera sits on the frame, and how far it's angled up, without
    /// recompiling. Raise the forward margin if the view clips into geometry or you want less
    /// prop in frame; lower it to sit further back and see more of the airframe.
    /// </summary>
    internal class DroneCam : CustomCommand
    {
        public override string Name => "Set Drone Camera Offset";
        public override string Description => "Sets how far forward, how high and how far tilted up the FPV camera sits on the frame. Usage: /dronecam <forward> [height] [uptilt]";
        public override string Format => "/dronecam <forward> [height] [uptilt]";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            if (message == null || message.Args.Count < 1 || !float.TryParse(message.Args[0], out float forward))
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                    $"Usage: {Format}  (current: fwd {DroneState.CameraForwardMargin:0.00}m, height {DroneState.CameraHeightOffset:0.00}m, uptilt {DroneState.CameraUpTilt:0}deg)");
                return;
            }

            DroneState.CameraForwardMargin = forward;
            if (message.Args.Count >= 2 && float.TryParse(message.Args[1], out float height))
            {
                DroneState.CameraHeightOffset = height;
            }
            if (message.Args.Count >= 3 && float.TryParse(message.Args[2], out float tilt))
            {
                DroneState.CameraUpTilt = tilt;
            }

            // Position and tilt are read every LateUpdate, so unlike the FOV these take effect on
            // a drone that's already in the air - handy for dialling the mount in mid-flight.
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Camera mount -> fwd {DroneState.CameraForwardMargin:0.00}m, height {DroneState.CameraHeightOffset:0.00}m, uptilt {DroneState.CameraUpTilt:0}deg");
        }
    }
}
