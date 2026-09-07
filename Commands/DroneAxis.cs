using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Remaps which detected joystick axis index drives which stick, without recompiling.
    /// Use /dronediag first to see the available indices.
    /// </summary>
    internal class DroneAxis : CustomCommand
    {
        public override string Name => "Set Drone Axis Mapping";
        public override string Description => "Maps a transmitter axis index to a stick. Usage: /droneaxis <roll|pitch|throttle|yaw> <index>";
        public override string Format => "/droneaxis <stick> <index>";
        public override string Category => "Drone";

        public override void Execute(CommandInput message)
        {
            if (message == null || message.Args.Count < 2 || !int.TryParse(message.Args[1], out int index))
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Usage: " + Format);
                return;
            }

            switch (message.Args[0].ToLowerInvariant())
            {
                case "roll":
                    DroneState.RollAxisIndex = index;
                    break;
                case "pitch":
                    DroneState.PitchAxisIndex = index;
                    break;
                case "throttle":
                    DroneState.ThrottleAxisIndex = index;
                    break;
                case "yaw":
                    DroneState.YawAxisIndex = index;
                    break;
                default:
                    CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Unknown stick. Use roll, pitch, throttle or yaw.");
                    return;
            }

            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage($"{message.Args[0]} -> axis {index}");
        }
    }
}
