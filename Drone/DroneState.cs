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

        // A drone costs whatever its payload costs - it IS a stick of TNT, just thrown by a
        // different mechanism, so charging a premium for the same item read as a bug. The stand
        // is left to price itself from the payload item (ItemPurchasable.Hover rebuilds its cost
        // from Item.Cost every frame), which also means the drone tracks the payload's price
        // automatically if PayloadItemName is pointed at something else.

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

        // Where the FPV camera is mounted on the drone frame, in the craft's local space. This
        // sits on the camera pod DroneModel builds at z=0.14, i.e. *behind* the front props
        // (whose hubs are at z=0.196) and below the prop disc, which is exactly why a real FPV
        // feed has prop tips flicking through the top corners - mount it out past the nose and
        // the whole airframe disappears behind the lens. Tune live with /dronecam if the view
        // clips into geometry (raise it) or you want the props further out of frame (raise the
        // forward margin).
        public static float CameraForwardMargin = 0.14f;
        public static float CameraHeightOffset = 0.035f;

        // Uptilt, degrees. Real FPV cams are angled up so the horizon sits low when the quad
        // pitches forward to fly fast; it also drops the prop line into the top of the frame.
        // 30 is a typical racing-quad angle - enough that the horizon stays in view at speed.
        // Set from the mod menu ("Drone Camera Uptilt") or with /dronetilt <degrees>.
        public static float CameraUpTilt = 30f;

        // Clamp range for the above. Past vertical the "uptilt" would flip the view over, and
        // negative angles are downtilt, which no FPV quad is mounted for.
        public const float MinCameraUpTilt = 0f;
        public const float MaxCameraUpTilt = 90f;

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
