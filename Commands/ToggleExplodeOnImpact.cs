using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Controls whether hitting terrain, a boat or a homing target detonates the drone. Off, the
    /// drone bounces off the world and keeps flying, so you can fly it around scouting instead of
    /// ending the flight on the first thing you clip.
    /// </summary>
    internal class ToggleExplodeOnImpact : CustomCommand
    {
        public override string Name => "Explode On Impact";

        public override string Description => "Whether hitting something detonates the drone, or it just keeps flying.";

        public override string Format => "/droneimpact";

        public override string Category => "Drone";

        public override bool IsToggle => true;

        // Proxy straight through to the shared flight state rather than keeping a backing field -
        // the menu flips IsEnabled directly (and LoadConfig writes to it at registration), so a
        // separate field would silently drift out of sync with what the drone actually reads.
        public override bool IsEnabled
        {
            get => DroneState.ExplodeOnImpact;
            set => DroneState.ExplodeOnImpact = value;
        }

        public override bool HasConfig => true;

        public override bool PersistConfig => true;

        public override void Execute(CommandInput message)
        {
            // IsEnabled has already been flipped by the menu/Handle before we get here.
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Explode on impact is now {(IsEnabled ? "on" : "off")}");
        }
    }
}
