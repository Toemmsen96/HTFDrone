using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// The drone shown on a shop stand, in place of whatever item the cloned stand used to
    /// display. Turns slowly on the spot with its props idling, so the stand reads as selling
    /// drones at a glance.
    /// </summary>
    internal class StandDroneDisplay : MonoBehaviour
    {
        // Bigger than the carried model - a shop display wants to be readable from a distance.
        private const float DisplayScale = 0.8f;

        private Transform _frameRoot;
        private Transform[] _props;
        private float _propSpin;

        public static void Attach(Transform mount)
        {
            if (!mount || (bool)mount.GetComponent<StandDroneDisplay>())
            {
                return;
            }
            mount.gameObject.AddComponent<StandDroneDisplay>();
        }

        private void Start()
        {
            _frameRoot = DroneModel.BuildFrame(transform, DisplayScale, out _props);
        }

        private void Update()
        {
            if (!_frameRoot)
            {
                return;
            }
            _frameRoot.localRotation *= Quaternion.Euler(0f, 30f * Time.deltaTime, 0f);
            DroneModel.SpinProps(_props, ref _propSpin, 200f);
        }

        private void OnDestroy()
        {
            if ((bool)_frameRoot)
            {
                Destroy(_frameRoot.gameObject);
            }
        }
    }
}
