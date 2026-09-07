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

        public static float MaxFlightTime = 20f;

        // Toggled from the mod menu (/droneinfinite, /droneimpact). Infinite flight ignores
        // MaxFlightTime entirely; explode-on-impact off lets the drone bounce off the world and
        // keep flying so you can scout with it instead of ending the flight on the first hit.
        public static bool InfiniteFlight;
        public static bool ExplodeOnImpact = true;

        // What a drone costs from the shop stand added to each island. Set on the cloned stand
        // rather than on the item prefab - the payload is a stock game item, and editing its Cost
        // would reprice every one of them in the world.
        public static int DronePrice = 250;

        // What a drone calls itself in your hands, on hover and on the shop stand. The payload is
        // a stock game item, so this is applied by patching Item.GetName for tagged instances
        // rather than by editing the shared prefab (which would rename every TNT in the world).
        public static string DroneItemName = "Drone";

        // Dress up other players' drones locally, so a modded player watching a modded pilot sees
        // an actual quad rather than a flying stick of TNT. Purely cosmetic and entirely local -
        // players without the mod are unaffected and still see (and are hurt by) a normal
        // explosive, so nothing about the shared game state depends on who has the mod.
        public static bool ShowOtherPlayersDrones = true;

        // Show the pilot's own body while flying. The local player's third-person model is
        // destroyed at spawn (only the first-person hands survive), so this puts a stand-in there
        // built from the death-ragdoll prefab - see PilotStandIn.
        public static bool ShowPilotBody = true;

        // Converting a held TNT into a drone (and back) rides the game's own "PlayerChangeSkin"
        // action - the Y/C skin-swap keys - rather than reading raw keys, which just fought that
        // existing binding. Nothing is lost by taking it over: explosives have no skins to cycle.
        // Rebinding therefore happens in the game's own controls, not here.

        // --- Real quad-style acro flight model ---
        // The drone has actual mass/gravity/drag on its Rigidbody. Throttle is thrust along the
        // drone's own up axis (like a real quad - full stick climbs, mid stick roughly hovers,
        // low stick falls). Pitch/roll/yaw are pure angular RATES (true acro/rate mode, not
        // angle/self-level mode): stick deflection sets how fast the frame spins on that axis,
        // integrated straight into attitude every tick with no auto-leveling and no tilt limit -
        // it can flip, loop and fly upside down exactly like a real acro quad. Linear motion is
        // flown with real AddForce, so momentum and drift are genuine physics, not scripted.
        public static float MaxThrust = 28f;          // full throttle force, in units of gravity acceleration (see HoverThrottle)
        public static float HoverThrottle = 0.55f;     // stick position (0-1) that exactly cancels gravity
        public static float MaxRateDegreesPerSecond = 360f; // full stick deflection = this many degrees/sec on that axis
        public static float LinearDrag = 0.6f;         // air resistance, keeps it from accelerating forever

        // Only one drone in flight at a time to keep things simple.
        public static bool DroneInFlight;

        // Where the FPV camera is mounted on the drone frame, in the craft's local space. The
        // frame DroneModel builds is ~0.3m long, so ~0.3m forward puts the lens just ahead of the
        // props like a real cam pod. Tune live with /dronecam if the view clips into geometry
        // (raise it) or feels like it's flying ahead of the craft (lower it).
        public static float CameraForwardMargin = 0.32f;
        public static float CameraHeightOffset = 0.05f;

        // FPV cams run wide - this is what gives that fisheye, everything-rushing-past feel.
        public static float CameraFov = 105f;

        // A nose-mounted cam can't see its own airframe, so the model is hidden from the pilot's
        // view (other players still see it). Turn off with /dronemodel to check the frame is
        // where you expect, or if you'd rather fly chase-cam style.
        // Default off: a real nose cam can't see its own frame, but with the camera mounted at
        // the nose the airframe is behind the lens anyway - and seeing it (plus the slung
        // payload) is far more useful than not. Toggle with /dronemodel.
        public static bool HideModelFromPilot = false;

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

        // Arm channel (e.g. QX7's SA/SF switch mapped to a gamepad button/trigger) - detonates
        // the drone in flight on the spot. Bound to a keyboard key by default too so it's usable
        // without a transmitter plugged in.
        public static KeyCode DetonateKey = KeyCode.Return;
    }
}
