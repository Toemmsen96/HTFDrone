
using System.Collections.Generic;
using CTDynamicModMenu.Commands;

namespace HTFDrone.Commands
{internal class Commands
    {
        public static List<CustomCommand> AllCommands { get; } = new List<CustomCommand>
        {
            new StartDrone(),
            new DroneDiag(),
            new DroneAxis(),
            new DroneStick()
        };
    }
}