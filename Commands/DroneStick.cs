using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Flips which physical stick (left/right) drives throttle+yaw vs pitch+roll, for
    /// transmitters detected as a standard Gamepad. Use /dronediag to check which stick is which
    /// while flying by hand.
    /// </summary>
    internal class DroneStick : CustomCommand
    {
        public override string Name => "Swap Drone Sticks";
        public override string Description => "Swaps which stick (left/right) is throttle+yaw vs pitch+roll, for gamepad-mode transmitters.";
        public override string Format => "/dronestick";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            DroneState.ThrottleYawOnLeftStick = !DroneState.ThrottleYawOnLeftStick;
            string side = DroneState.ThrottleYawOnLeftStick ? "left" : "right";
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage($"Throttle+Yaw now on {side} stick.");
        }
    }
}
