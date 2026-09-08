
using System.Collections.Generic;
using CTDynamicModMenu.Commands;

namespace HTFDrone.Commands
{internal class Commands
    {
        public static List<CustomCommand> AllCommands { get; } = new List<CustomCommand>
        {
            new StartDrone(),
            new DroneDiag(),
            new DroneStandDiag(),
            new DroneAxis(),
            new DroneStick(),
            new DroneCam(),
            new DroneTilt(),
            new DroneFlightSettings(),
            new DroneCameraSettings(),
            new DroneInputSettings(),
            new DroneModelToggle(),
            new ToggleInfiniteFlight(),
            new ToggleExplodeOnImpact(),
            new ToggleRemoteDrones()
        };
    }
}