using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Flies a spawned Explosive item like a real acro-mode FPV quad: throttle is thrust
    /// (AddForce) along its own up axis against real gravity - mid-stick roughly hovers, full
    /// stick climbs, low stick falls - while roll/pitch/yaw are pure angular RATES (real acro,
    /// not angle/self-level mode): stick deflection sets how fast the frame spins on that axis
    /// and it just keeps spinning for as long as you hold it, with no auto-leveling and no tilt
    /// limit - you can flip and loop it same as a real acro quad. Linear motion is genuine
    /// Rigidbody physics with drag/momentum. Detonates on impact or on timeout - there is no
    /// auto-targeting, it goes exactly where you fly it.
    /// </summary>
    internal class KamikazeDrone : MonoBehaviour
    {
        private Item _item;
        private Rigidbody _rig;
        private Player _pilot;
        private float _timeAlive;
        private bool _detonated;
        private Renderer[] _renderers;
        private DroneAudio _audio;
        private PayloadMount _payloadMount;
        private PilotStandIn _pilotStandIn;

        // Last tick's position, for the cross-tick impact sweep (see FixedUpdate).
        private Vector3 _prevPosition;
        private bool _hasPrevPosition;

        public static KamikazeDrone Launch(Item item, Player pilot, Vector3 initialForward)
        {
            KamikazeDrone drone = item.gameObject.AddComponent<KamikazeDrone>();
            drone._item = item;
            drone._pilot = pilot;
            drone._item.transform.rotation = Quaternion.LookRotation(initialForward.normalized, Vector3.up);

            // The pilot's position is "home" for the OSD's distance readout and return arrow.
            if ((bool)pilot)
            {
                DroneOsd.HomePosition = pilot.Transform.position;
                DroneOsd.HasHome = true;
            }
            return drone;
        }

        private void Start()
        {
            _rig = _item.Rig;

            // If this was a drone being carried, drop the carried airframe before taking the
            // renderer snapshot below - otherwise its frame gets captured as "the item's mesh",
            // gets hidden forever, and the flight model stacks a second quad on top of it.
            HeldDroneModel.Remove(_item);

            // Captured before DroneModel adds the visible quad frame below, so this array only
            // ever holds the payload item's own renderers. HideRenderers() re-applies to exactly
            // this list every tick - don't re-query it later or it would hide our own model too.
            _renderers = _item.GetComponentsInChildren<Renderer>(includeInactive: true);
            HideRenderers();

            // The payload rides under the airframe as a visible slung shell. This reuses the
            // item's own mesh rather than modelling one - it IS the explosive being carried, so
            // it's guaranteed to look like the game's TNT and to match whatever payload is used.
            _payloadMount = PayloadMount.Attach(_item, _renderers);

            if ((bool)_item.RigidbodySync)
            {
                _item.RigidbodySync.StartSimulateLocal();
            }
            if ((bool)_rig)
            {
                _rig.useGravity = true;
                _rig.linearDamping = DroneState.LinearDrag;
                // Attitude is driven directly via MoveRotation, not torque, so angular drag on
                // the Rigidbody wouldn't do anything - freeze physics-driven spin entirely so a
                // stray collision impulse can't out-rotate our own MoveRotation calls.
                _rig.angularVelocity = Vector3.zero;
                _rig.constraints = RigidbodyConstraints.FreezeRotation;
                // Kick off with a bit of forward+up thrust so it doesn't just drop like a rock
                // for the first tick before the pilot takes over.
                _rig.linearVelocity = _item.transform.forward * 4f;
            }

            // The payload item's own mesh stays hidden (see HideRenderers) - what you actually see
            // flying is this quad frame, built at runtime and parented to the same transform.
            DroneModel model = DroneModel.Attach(gameObject);
            _audio = DroneAudio.Attach(gameObject);

            if (_pilot == Player.LocalPlayer)
            {
                // The pilot is looking out of the nose cam, which physically can't see its own
                // airframe - showing it would just put the drone in the middle of the feed.
                // Everyone else still sees the quad fly past.
                model.SetVisible(!DroneState.HideModelFromPilot);
                DroneCamera.Attach(gameObject, _pilot);

                // Give the pilot a body to look back at - they have no third-person model of
                // their own, so without this the drone feed shows floating hands.
                _pilotStandIn = PilotStandIn.Create(_pilot);
            }
        }

        private void FixedUpdate()
        {
            if (_detonated || !_item || !_rig)
            {
                return;
            }

            // Some other system (network spawn init, RigidbodySync activity toggling, etc.) can
            // flip child renderers back on after our one-time disable in Start() - keep stamping
            // them off every tick rather than trying to win a timing race.
            HideRenderers();

            // Same reasoning as the camera FOV: drag is applied once at spawn, so re-stamp it
            // each tick or a drag slider moved in flight would sit dead until the next launch.
            if (!Mathf.Approximately(_rig.linearDamping, DroneState.LinearDrag))
            {
                _rig.linearDamping = DroneState.LinearDrag;
            }

            _timeAlive += Time.fixedDeltaTime;
            if (!DroneState.InfiniteFlight && _timeAlive >= DroneState.MaxFlightTime)
            {
                Detonate();
                return;
            }

            Vector3 position = _item.transform.position;

            // Ditching in the water always ends the flight - a quad that's gone in the drink is
            // gone either way, and leaving it running underwater with no way to recover it would
            // just strand the pilot's camera down there.
            if (WaterManager.GetWaterHeight(position) > position.y)
            {
                Detonate();
                return;
            }

            if (DroneState.ManualControlEnabled && TransmitterInput.DetonatePressed())
            {
                Detonate();
                return;
            }

            TransmitterInput.Sticks sticks = DroneState.ManualControlEnabled ? TransmitterInput.Read() : default;
            DroneOsd.LastSticks = sticks;

            // Pure acro/rate mode, always hand-flown. Stick deflection is an angular velocity,
            // not a target angle: hold the stick over and it keeps spinning on that axis - no
            // leveling, no tilt cap. Centre the stick and rotation on that axis stops (rate = 0),
            // it does NOT spring back upright.
            //
            // Read unconditionally rather than only when HasInput: centred sticks are a valid
            // command (hold this attitude, throttle wherever it's set), and gating on HasInput
            // would read throttle as zero the moment you stopped moving the sticks and drop the
            // drone out of the sky.
            float pitchRate = sticks.Pitch * DroneState.MaxRateDegreesPerSecond;
            float rollRate = -sticks.Roll * DroneState.MaxRateDegreesPerSecond;
            float yawRate = sticks.Yaw * DroneState.MaxRateDegreesPerSecond;
            float throttle01 = Mathf.Clamp01((sticks.Throttle + 1f) * 0.5f);

            ApplyFlightModel(pitchRate, rollRate, yawRate, throttle01);

            if ((bool)_audio)
            {
                _audio.SetThrottle(throttle01);
            }

            DroneOsd.TimeAlive = _timeAlive;

            // Sweep from where we were on the PREVIOUS tick to where we are now, so a fast drone
            // can't tunnel through a wall between physics steps. It has to span ticks like this:
            // ApplyFlightModel only queues forces, and Unity doesn't integrate them until after
            // FixedUpdate returns, so comparing against a position captured earlier in this same
            // call would always measure a distance of zero and never detect anything.
            // Skipped entirely when explode-on-impact is off - then the drone just bounces off
            // the world under its own collider and flies on.
            if (DroneState.ExplodeOnImpact && _hasPrevPosition)
            {
                Vector3 moved = position - _prevPosition;
                float travelDist = moved.magnitude;
                if (travelDist > 0.001f
                    && Physics.Raycast(_prevPosition, moved.normalized, out RaycastHit hit, travelDist, (int)GameInfo.LevelLayer | (int)GameInfo.BoatLayer))
                {
                    _item.transform.position = hit.point;
                    Detonate();
                    return;
                }
            }

            _prevPosition = position;
            _hasPrevPosition = true;
        }

        /// <summary>
        /// Applies the actual quad physics for this tick: spins the frame on all three axes by
        /// the commanded rates (real acro - the rates just integrate into attitude, nothing pulls
        /// it back level or clamps how far over it can go) and pushes it along its own up axis by
        /// the commanded throttle.
        /// </summary>
        private void ApplyFlightModel(float pitchRate, float rollRate, float yawRate, float throttle01)
        {
            // Local-space rotation, so these are always the drone's OWN current pitch/roll/yaw
            // axes regardless of how it's currently oriented - what lets it tumble/flip/loop
            // through any attitude instead of being pulled back towards level.
            _rig.MoveRotation(_item.transform.rotation * Quaternion.Euler(pitchRate * Time.fixedDeltaTime, yawRate * Time.fixedDeltaTime, rollRate * Time.fixedDeltaTime));

            // Thrust along the frame's own (possibly upside-down!) up axis - this is what makes
            // attitude translate into actual drift/movement instead of just spinning in place,
            // same as a real quad. Hover throttle exactly cancels gravity when level; above/below
            // that scales linearly from "no thrust" to "max thrust".
            float hoverForce = Physics.gravity.magnitude * _rig.mass;
            float thrust = (throttle01 <= DroneState.HoverThrottle)
                ? Mathf.Lerp(0f, hoverForce, throttle01 / Mathf.Max(DroneState.HoverThrottle, 0.0001f))
                : Mathf.Lerp(hoverForce, DroneState.MaxThrust * _rig.mass, (throttle01 - DroneState.HoverThrottle) / Mathf.Max(1f - DroneState.HoverThrottle, 0.0001f));

            _rig.AddForce(_item.transform.up * thrust, ForceMode.Force);
        }

        /// <summary>
        /// Backstop for the movement sweep in FixedUpdate. That sweep only sees geometry crossed
        /// *between* two tick positions, so a drone pinned against a wall by its own thrust -
        /// travelling ~0 per tick while very much in contact - would never trip it. Unity's own
        /// collision callback catches exactly that case.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (_detonated || !DroneState.ExplodeOnImpact || collision == null)
            {
                return;
            }

            // Only the world detonates it, matching the layers the movement sweep tests. Without
            // this it would also go off against the player who launched it, floating fish, and
            // anything else it brushes on the way up.
            int hitLayer = 1 << collision.gameObject.layer;
            int worldLayers = (int)GameInfo.LevelLayer | (int)GameInfo.BoatLayer;
            if ((hitLayer & worldLayers) == 0)
            {
                return;
            }

            Detonate();
        }

        private void HideRenderers()
        {
            if (_renderers == null)
            {
                return;
            }
            foreach (Renderer renderer in _renderers)
            {
                // The slung payload is drawn from these same renderers, so leave those visible -
                // they're the shell hanging under the airframe, not leftover TNT.
                if ((bool)_payloadMount && _payloadMount.IsMountedRenderer(renderer))
                {
                    continue;
                }
                if ((bool)renderer)
                {
                    renderer.enabled = false;
                }
            }
        }

        private void Detonate()
        {
            if (_detonated || !_item)
            {
                return;
            }
            _detonated = true;
            DroneState.DroneInFlight = false;
            if ((bool)_item.Explosive)
            {
                _item.Explosive.ForceExplode(_pilot, instant: true);
            }

            // Switch the view back to the player right away rather than waiting for the item's
            // eventual network despawn to take the DroneCamera component down with it.
            DroneCamera cam = GetComponent<DroneCamera>();
            if ((bool)cam)
            {
                Destroy(cam);
            }

            // Kill the frame and the motor whine at the same moment, so a silent invisible
            // airframe isn't left flying on after the explosion.
            DroneModel model = GetComponent<DroneModel>();
            if ((bool)model)
            {
                Destroy(model);
            }
            if ((bool)_audio)
            {
                Destroy(_audio);
            }

            // The pilot gets their own body back the moment the view returns to them.
            if ((bool)_pilotStandIn)
            {
                Destroy(_pilotStandIn.gameObject);
            }
            Destroy(this);
        }

        private void OnDestroy()
        {
            DroneState.DroneInFlight = false;

            // Safety net for the paths that don't go through Detonate (network despawn, scene
            // change) - otherwise a stand-in body would be left standing at the pilot forever.
            if ((bool)_pilotStandIn)
            {
                Destroy(_pilotStandIn.gameObject);
            }
        }
    }
}
