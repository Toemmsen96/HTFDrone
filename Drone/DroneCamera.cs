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

            GameObject camObj = new GameObject("DroneFpvCamera");
            camObj.transform.SetParent(transform, worldPositionStays: false);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;

            _droneCam = camObj.AddComponent<Camera>();
            if ((bool)_playerCam)
            {
                _droneCam.CopyFrom(_playerCam);
            }
            _droneCam.enabled = true;

            _pilot.SetCurCam(_droneCam);
            if ((bool)_playerCam)
            {
                _playerCam.enabled = false;
            }
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
