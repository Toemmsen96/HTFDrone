using CTDynamicModMenu.Commands;
using HTFDrone.Drone;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Whether other players' drones are drawn as drones here. Purely local cosmetics - it has no
    /// effect on anyone else's game, and players without the mod always see a plain explosive.
    /// </summary>
    internal class ToggleRemoteDrones : CustomCommand
    {
        // No apostrophe: CustomCommand.LoadConfig binds a BepInEx config key built from this
        // name, and BepInEx rejects ' " = [ ] \ and whitespace escapes in key names - an
        // apostrophe here throws during RegisterCommand and aborts the rest of Plugin.Awake.
        public override string Name => "Show Other Players Drones";

        public override string Description => "Draws drones flown by other modded players as drones instead of flying TNT.";

        public override string Format => "/droneothers";

        public override string Category => "Drone";

        public override bool IsToggle => true;

        public override bool IsEnabled
        {
            get => DroneState.ShowOtherPlayersDrones;
            set => DroneState.ShowOtherPlayersDrones = value;
        }

        public override bool HasConfig => true;

        public override bool PersistConfig => true;

        public override void Execute(CommandInput message)
        {
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(
                $"Showing other players' drones is now {(IsEnabled ? "on" : "off")}");
        }
    }
}
