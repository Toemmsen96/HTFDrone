using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Tunes where the FPV camera sits on the frame, without recompiling. Raise the forward
    /// margin if the view clips into geometry or you want less prop in frame; lower it to sit
    /// further back and see more of the airframe. The uptilt has its own command (/dronetilt) so
    /// there's one owner for it rather than two places that can disagree.
    /// </summary>
    internal class DroneCam : CustomCommand
    {
        public override string Name => "Set Drone Camera Offset";
        public override string Description => "Sets how far forward and how high the FPV camera sits on the frame. Usage: /dronecam <forward> [height]";
        public override string Format => "/dronecam <forward> [height]";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            if (message == null || message.Args.Count < 1 || !float.TryParse(message.Args[0], out float forward))
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                    $"Usage: {Format}  (current: fwd {DroneState.CameraForwardMargin:0.00}m, height {DroneState.CameraHeightOffset:0.00}m)");
                return;
            }

            DroneState.CameraForwardMargin = forward;
            if (message.Args.Count >= 2 && float.TryParse(message.Args[1], out float height))
            {
                DroneState.CameraHeightOffset = height;
            }

            // The mount offset is read every LateUpdate, so unlike the FOV this takes effect on a
            // drone that's already in the air - handy for dialling it in mid-flight.
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Camera mount -> fwd {DroneState.CameraForwardMargin:0.00}m, height {DroneState.CameraHeightOffset:0.00}m");
        }
    }
}
