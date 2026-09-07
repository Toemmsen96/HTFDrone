using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Tunes how far forward of the model's nose the FPV camera sits, without recompiling.
    /// Raise it if the view still clips into geometry before the body visibly would; lower it if
    /// it feels like the view is flying ahead of the drone.
    /// </summary>
    internal class DroneCam : CustomCommand
    {
        public override string Name => "Set Drone Camera Offset";
        public override string Description => "Sets how far forward (and optionally how high) the FPV camera sits on the frame. Usage: /dronecam <forward> [height]";
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

            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Camera mount -> fwd {DroneState.CameraForwardMargin:0.00}m, height {DroneState.CameraHeightOffset:0.00}m (applies on next /fpvdrone)");
        }
    }
}
