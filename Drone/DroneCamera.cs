using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Switches the local player's view to a camera riding on the drone (true FPV feed) with a
    /// simple text OSD, and switches back to the player's own camera when the drone dies. Built
    /// entirely at runtime - no scene-authored camera/canvas prefab needed. Mirrors how
    /// PlayerDeathCam swaps the active view: enable our Camera, call Player.SetCurCam, disable
    /// the player's own Camera component (not the whole PlayerCamera, so its look logic keeps
    /// running untouched for when we hand control back).
    /// </summary>
    internal class DroneCamera : MonoBehaviour
    {
        private Player _pilot;
        private Camera _droneCam;
        private Camera _playerCam;
        private Rigidbody _droneRig;
        private bool _restored;

        public static DroneCamera Attach(GameObject droneObject, Player pilot)
        {
            DroneCamera view = droneObject.AddComponent<DroneCamera>();
            view._pilot = pilot;
            view._droneRig = droneObject.GetComponent<Rigidbody>();
            return view;
        }

        private void Start()
        {
            _playerCam = _pilot.Camera ? _pilot.Camera.Cam : null;

            // Deliberately NOT parented to the drone. Item prefabs carry their own (often small,
            // often non-uniform) root scale, which would both shrink the mount offset and squash
            // the camera - that's what left the view sitting inside/behind the frame instead of
            // out at the nose. Keeping it unparented at scale 1 and driving it from the drone's
            // world pose each frame makes the mount offset mean real metres.
            GameObject camObj = new GameObject("DroneFpvCamera");
            camObj.transform.localScale = Vector3.one;

            _droneCam = camObj.AddComponent<Camera>();
            if ((bool)_playerCam)
            {
                _droneCam.CopyFrom(_playerCam);
            }
            // CopyFrom brings the player camera's near plane with it, which is tuned for a
            // human-height view and would slice through the drone frame right in front of us.
            // It has to clear the closest thing we deliberately want on screen - the front prop
            // tips, which sweep to within ~0.06m of the lens - so keep it well under that.
            _droneCam.nearClipPlane = 0.01f;
            _droneCam.fieldOfView = DroneState.CameraFov;

            // Note on seeing the pilot: the local player has no third-person body to render.
            // Player.InitializePlayer *destroys* every object in _otherObjects for the local
            // client (keeping only the first-person _localObjects, i.e. the hands), so this isn't
            // a culling mask we can re-enable - the model genuinely doesn't exist. Showing the
            // pilot needs a stand-in built for the purpose; see PilotStandIn.
            _droneCam.enabled = true;

            _pilot.SetCurCam(_droneCam);
            if ((bool)_playerCam)
            {
                _playerCam.enabled = false;
            }
        }

        /// <summary>
        /// Pin the lens to the drone's nose in world space. Runs in LateUpdate so it lands after
        /// physics and any other transform writes for the frame - doing it earlier would leave
        /// the view a frame behind the craft, which reads as floating/lagging.
        /// </summary>
        private void LateUpdate()
        {
            if (!_droneCam)
            {
                return;
            }

            // Offset in the drone's own axes, but applied as real metres in world space so the
            // payload prefab's root scale can't shrink it.
            Vector3 mount = transform.position
                + transform.forward * DroneState.CameraForwardMargin
                + transform.up * DroneState.CameraHeightOffset;

            // Uptilt is applied around the drone's own right axis, so it stays a fixed angle on
            // the airframe through rolls and flips rather than drifting toward world-up.
            Quaternion tilt = Quaternion.AngleAxis(-DroneState.CameraUpTilt, transform.right);

            _droneCam.transform.SetPositionAndRotation(mount, tilt * transform.rotation);
        }

        private void OnGUI()
        {
            if (!_droneCam)
            {
                return;
            }
            DroneOsd.Draw(_droneRig, transform);
        }

        private void RestorePlayerCamera()
        {
            if (_restored)
            {
                return;
            }
            _restored = true;
            if ((bool)_playerCam)
            {
                _playerCam.enabled = true;
                _pilot.SetCurCam(_playerCam);
            }
        }

        private void OnDestroy()
        {
            RestorePlayerCamera();
            if ((bool)_droneCam)
            {
                Destroy(_droneCam.gameObject);
            }
        }
    }
}
