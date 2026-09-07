using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Flies a spawned Explosive item forward from where it was launched, gently homing onto
    /// whatever Creature/Player is roughly in front of it, and detonates on arrival or on
    /// running into terrain/water/time-out. No custom model or projectile system needed - it
    /// just steers the item's own Rigidbody like the game's AttackingFish does to itself.
    /// </summary>
    internal class KamikazeDrone : MonoBehaviour
    {
        private Item _item;
        private Player _pilot;
        private Vector3 _direction;
        private Transform _target;
        private float _timeAlive;
        private float _flightSpeed;
        private bool _detonated;

        public static KamikazeDrone Launch(Item item, Player pilot, Vector3 direction)
        {
            KamikazeDrone drone = item.gameObject.AddComponent<KamikazeDrone>();
            drone._item = item;
            drone._pilot = pilot;
            drone._direction = direction.normalized;
            return drone;
        }

        private void Start()
        {
            // RigidbodySync clamps maxLinearVelocity to GameInfo.MaxItemVel every FixedUpdate;
            // stay under that so our commanded velocity isn't silently capped below what we ask for.
            _flightSpeed = Mathf.Min(DroneState.FlightSpeed, GameInfo.MaxItemVel > 0f ? GameInfo.MaxItemVel : DroneState.FlightSpeed);

            if ((bool)_item.RigidbodySync)
            {
                _item.RigidbodySync.StartSimulateLocal();
            }
            if ((bool)_item.Rig)
            {
                _item.Rig.useGravity = false;
                _item.Rig.linearVelocity = _direction * _flightSpeed;
            }

            if (_pilot == Player.LocalPlayer)
            {
                DroneCamera.Attach(gameObject, _pilot);
            }
        }

        private void FixedUpdate()
        {
            if (_detonated || !_item || !_item.Rig)
            {
                return;
            }

            _timeAlive += Time.fixedDeltaTime;
            if (_timeAlive >= DroneState.MaxFlightTime)
            {
                Detonate();
                return;
            }

            Vector3 position = _item.transform.position;

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

            bool reachedTarget = sticks.HasInput ? FlyManual(sticks, position) : FlyAutopilot(position);
            if (reachedTarget)
            {
                Detonate();
                return;
            }

            _item.Rig.linearVelocity = _direction * _flightSpeed;
            _item.transform.rotation = Quaternion.LookRotation(_direction);

            DroneOsd.TimeAlive = _timeAlive;
            DroneOsd.CurrentTarget = _target;
            DroneOsd.ManualControl = sticks.HasInput;

            if (Physics.Raycast(position, _direction, out RaycastHit hit, _flightSpeed * Time.fixedDeltaTime, (int)GameInfo.LevelLayer | (int)GameInfo.BoatLayer))
            {
                _item.transform.position = hit.point;
                Detonate();
            }
        }

        /// <summary>
        /// Pilot has stick input - fly by hand, FPV style: roll/yaw turn the heading left-right,
        /// pitch tilts it up/down, throttle sets forward speed. No homing while hand-flown.
        /// </summary>
        private bool FlyManual(TransmitterInput.Sticks sticks, Vector3 position)
        {
            _target = null;

            float yawDelta = (sticks.Roll + sticks.Yaw) * DroneState.ManualTurnDegreesPerSecond * Time.fixedDeltaTime;
            float pitchDelta = -sticks.Pitch * DroneState.ManualTurnDegreesPerSecond * Time.fixedDeltaTime;

            Quaternion turn = Quaternion.AngleAxis(yawDelta, Vector3.up) * Quaternion.AngleAxis(pitchDelta, _item.transform.right);
            _direction = (turn * _direction).normalized;

            float throttle01 = Mathf.Clamp01((sticks.Throttle + 1f) * 0.5f);
            _flightSpeed = Mathf.Lerp(DroneState.ManualMinThrottleSpeed, DroneState.ManualMaxThrottleSpeed, throttle01);
            _flightSpeed = Mathf.Min(_flightSpeed, GameInfo.MaxItemVel > 0f ? GameInfo.MaxItemVel : _flightSpeed);

            return false;
        }

        /// <summary>Sticks centered (or no transmitter) - gently home onto the nearest target in front.</summary>
        private bool FlyAutopilot(Vector3 position)
        {
            if (!IsValidTarget(_target))
            {
                _target = DroneTargetFinder.FindTarget(position, _direction, DroneState.HomingFov, DroneState.HomingMaxDistance);
            }

            if (!IsValidTarget(_target))
            {
                return false;
            }

            Vector3 toTarget = _target.position - position;
            if (toTarget.magnitude <= DroneState.DetonateDistance)
            {
                return true;
            }
            _direction = Vector3.RotateTowards(_direction, toTarget.normalized, DroneState.TurnDegreesPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
            return false;
        }

        private bool IsValidTarget(Transform candidate)
        {
            return candidate && candidate.gameObject.activeInHierarchy;
        }

        private void Detonate()
        {
            if (_detonated || !_item)
            {
                return;
            }
            _detonated = true;
            DroneState.DroneInFlight = false;
            DroneOsd.CurrentTarget = null;
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
            Destroy(this);
        }

        private void OnDestroy()
        {
            DroneState.DroneInFlight = false;
        }
    }
}
