using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Sliders for the acro flight model - how hard it climbs, where it hovers, how fast it
    /// rotates and how quickly it bleeds off speed. All four are read every physics tick (drag
    /// is re-stamped onto the Rigidbody in KamikazeDrone.FixedUpdate for exactly this reason),
    /// so dragging a slider retunes a drone that is already in the air.
    /// </summary>
    internal class DroneFlightSettings : CustomCommand
    {
        public override string Name => "Drone Flight Model";
        public override string Description => "Thrust, hover point, rotation rate and drag. Takes effect immediately, even mid-flight.";
        public override string Format => "/droneflight";
        public override string Category => "Drone";

        public override bool HasConfig => true;
        public override bool PersistConfig => true;

        public DroneFlightSettings()
        {
            // Upper bound is deliberately well past the 28 default: over-powered is a
            // legitimate way to fly this, and a slider you can't push past stock is no fun.
            AddSlider(new CommandSlider(
                "Max Thrust", 5f, 80f, DroneState.MaxThrust, 0.5f, false,
                "Full-throttle thrust, in units of gravity",
                value => DroneState.MaxThrust = value));

            // Kept off both extremes: at 0 the throttle curve's lower half vanishes and at 1
            // the upper half does, either of which makes the stick feel broken rather than
            // powerful. KamikazeDrone guards the division anyway, but the range says the intent.
            AddSlider(new CommandSlider(
                "Hover Throttle", 0.1f, 0.9f, DroneState.HoverThrottle, 0.01f, false,
                "Stick position (0-1) that exactly cancels gravity",
                value => DroneState.HoverThrottle = value));

            AddSlider(new CommandSlider(
                "Max Rate", 60f, 1200f, DroneState.MaxRateDegreesPerSecond, 10f, false,
                "Degrees per second at full stick deflection on pitch/roll/yaw",
                value => DroneState.MaxRateDegreesPerSecond = value));

            AddSlider(new CommandSlider(
                "Linear Drag", 0f, 3f, DroneState.LinearDrag, 0.05f, false,
                "Air resistance - higher stops faster, 0 coasts forever",
                value => DroneState.LinearDrag = value));

            AddSlider(new CommandSlider(
                "Flight Time", 5f, 300f, DroneState.MaxFlightTime, 5f, false,
                "Seconds before the drone self-destructs (ignored while infinite flight is on)",
                value => DroneState.MaxFlightTime = value));
        }

        /// <summary>
        /// The base class restores slider values silently (SetValueSilent skips OnValueChanged),
        /// so without this the persisted numbers would show in the menu while DroneState kept
        /// its compile-time defaults.
        /// </summary>
        public override void LoadCustomConfig()
        {
            DroneState.MaxThrust = GetSliderValue("Max Thrust", DroneState.MaxThrust);
            DroneState.HoverThrottle = GetSliderValue("Hover Throttle", DroneState.HoverThrottle);
            DroneState.MaxRateDegreesPerSecond = GetSliderValue("Max Rate", DroneState.MaxRateDegreesPerSecond);
            DroneState.LinearDrag = GetSliderValue("Linear Drag", DroneState.LinearDrag);
            DroneState.MaxFlightTime = GetSliderValue("Flight Time", DroneState.MaxFlightTime);
        }

        public override void Execute(CommandInput message)
        {
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Thrust {DroneState.MaxThrust:0.0}g, hover {DroneState.HoverThrottle:0.00}, " +
                $"rate {DroneState.MaxRateDegreesPerSecond:0}deg/s, drag {DroneState.LinearDrag:0.00}, " +
                $"flight {DroneState.MaxFlightTime:0}s");
        }
    }
}
