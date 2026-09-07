using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Removes the flight timer, so the drone keeps flying until you crash it or detonate it by
    /// hand instead of self-destructing after DroneState.MaxFlightTime seconds.
    /// </summary>
    internal class ToggleInfiniteFlight : CustomCommand
    {
        public override string Name => "Infinite Flight";

        public override string Description => "Stops the drone self-destructing after its flight timer runs out.";

        public override string Format => "/droneinfinite";

        public override string Category => "Drone";

        public override bool IsToggle => true;

        // Proxy straight through to the shared flight state rather than keeping a backing field -
        // the menu flips IsEnabled directly (and LoadConfig writes to it at registration), so a
        // separate field would silently drift out of sync with what the drone actually reads.
        public override bool IsEnabled
        {
            get => DroneState.InfiniteFlight;
            set => DroneState.InfiniteFlight = value;
        }

        public override bool HasConfig => true;

        public override bool PersistConfig => true;

        public override void Execute(CommandInput message)
        {
            // IsEnabled has already been flipped by the menu/Handle before we get here.
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Infinite flight is now {(IsEnabled ? "on" : "off")}");
        }
    }
}
