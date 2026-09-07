using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Toggles whether the pilot sees the drone's own airframe. Off (the default) is the real FPV
    /// behaviour - a nose cam can't see its own frame. Turn it on to check where the model
    /// actually sits relative to the camera, or to fly it chase-cam style.
    /// </summary>
    internal class DroneModelToggle : CustomCommand
    {
        public override string Name => "Toggle Drone Model";
        public override string Description => "Shows/hides the drone's own airframe in your FPV view.";
        public override string Format => "/dronemodel";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            DroneState.HideModelFromPilot = !DroneState.HideModelFromPilot;
            string state = DroneState.HideModelFromPilot ? "hidden" : "visible";
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Drone model {state} in FPV view (applies on next /fpvdrone)");
        }
    }
}
