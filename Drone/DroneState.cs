using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Shared, tweakable settings for the FPV kamikaze drone. No physical drone model is used -
    /// the "drone" is one of the game's own Explosive items (e.g. dynamite) flown by hand.
    /// </summary>
    internal static class DroneState
    {
        // Name of the spawnable Item (GameInfo.GetSpawnable key: lowercase, no spaces) to use as
        // the drone's payload. If not found at spawn time, we fall back to scanning all spawnable
        // items for the first one that has an Explosive component.
        public static string PayloadItemName = "dynamite";

        public static float FlightSpeed = 18f;
        public static float TurnDegreesPerSecond = 220f;
        public static float MaxFlightTime = 12f;

        // How close the drone needs to get to its locked target (or the aim point, if no
        // creature/player is in range) before it detonates.
        public static float DetonateDistance = 1.25f;

        // How wide a cone in front of the drone to search for a homing target.
        public static float HomingFov = 25f;
        public static float HomingMaxDistance = 120f;

        // Only one drone in flight at a time to keep things simple.
        public static bool DroneInFlight;

        // --- Taranis QX7 (or any joystick-mode RC transmitter) manual control ---
        // The game's shipped Input Manager has no joystick axes registered, so legacy
        // Input.GetAxis can't see any transmitter/gamepad - we go through Unity's new Input
        // System instead (already used by the game itself). Depending on OS/USB mode, the QX7
        // can enumerate either as a generic HID Joystick (raw indexed axes) or as a standard
        // Gamepad (left/right stick + triggers) - most USB modes report as the latter, so that's
        // tried first. Run "/dronediag" in-game to see what was detected and its live values.
        public static bool ManualControlEnabled = true;

        // Which physical stick maps to which gamepad control. Defaults match a Taranis QX7 in
        // Mode 2 reporting as a standard gamepad: left stick vertical = throttle, left stick
        // horizontal = yaw/rudder, right stick vertical = pitch, right stick horizontal = roll.
        public static bool ThrottleYawOnLeftStick = true;

        // Fallback for transmitters that enumerate as a raw HID Joystick instead of a Gamepad:
        // index into the joystick's axis-typed controls (0-based, in HID report order).
        public static int RollAxisIndex = 3;
        public static int PitchAxisIndex = 2;
        public static int ThrottleAxisIndex = 1;
        public static int YawAxisIndex = 0;

        public static float StickDeadzone = 0.08f;
        public static float ManualTurnDegreesPerSecond = 260f;
        public static float ManualMinThrottleSpeed = 6f;
        public static float ManualMaxThrottleSpeed = 26f;

        // Arm channel (e.g. QX7's SA/SF switch mapped to a gamepad button/trigger) - detonates
        // the drone in flight on the spot. Bound to a keyboard key by default too so it's usable
        // without a transmitter plugged in.
        public static KeyCode DetonateKey = KeyCode.Return;
    }
}
