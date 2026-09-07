using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// The visible airframe on a drone you're carrying (bought from a stand, or converted from a
    /// TNT with the convert key). Hides the payload item's own mesh and shows the quad in its
    /// place, so a drone looks like a drone in your hands and on the ground - not like a stick of
    /// TNT that happens to behave differently when thrown.
    /// </summary>
    internal class HeldDroneModel : MonoBehaviour
    {
        // The carried quad is shown smaller than the flying one - at full size the airframe is
        // wider than the player's hands and clips through the view.
        private const float CarriedScale = 0.45f;

        private Transform _frameRoot;
        private Transform[] _props;
        private Renderer[] _itemRenderers;
        private PayloadMount _payloadMount;
        private float _propSpin;

        public static void Apply(Item item)
        {
            if (!item || (bool)item.GetComponent<HeldDroneModel>())
            {
                return;
            }
            item.gameObject.AddComponent<HeldDroneModel>();
        }

        public static void Remove(Item item)
        {
            if (!item)
            {
                return;
            }
            HeldDroneModel model = item.GetComponent<HeldDroneModel>();
            if ((bool)model)
            {
                // DestroyImmediate rather than Destroy: callers (notably KamikazeDrone.Start)
                // snapshot the item's renderers straight after this, and a deferred destroy would
                // leave the carried frame in that snapshot and re-enable the TNT mesh afterwards.
                model.Teardown();
                DestroyImmediate(model);
            }
        }

        private void Start()
        {
            // Cache the payload's own renderers before building ours, so the two never get mixed
            // up - re-querying later would find the drone frame and hide that too.
            _itemRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _frameRoot = DroneModel.BuildFrame(transform, CarriedScale, out _props);

            // Sling the TNT under the carried airframe rather than hiding it, so a drone in your
            // hands visibly carries its payload the same way it does in flight.
            Item item = GetComponent<Item>();
            if ((bool)item)
            {
                _payloadMount = PayloadMount.Attach(item, _itemRenderers, CarriedScale);
            }
            if (!_payloadMount)
            {
                SetItemRenderersEnabled(false);
            }
        }

        private void Update()
        {
            // Idle spin - slower than in flight, so a carried drone reads as armed but not flying.
            DroneModel.SpinProps(_props, ref _propSpin, 400f);
        }

        private void OnDestroy()
        {
            Teardown();
        }

        /// <summary>
        /// Puts the TNT's own model back and removes the quad frame, so converting away from a
        /// drone (or the item being reused) doesn't leave an invisible item behind. Safe to call
        /// twice - Remove() calls it explicitly before a DestroyImmediate, which then triggers
        /// OnDestroy as well.
        /// </summary>
        private void Teardown()
        {
            // The mount goes first: it restores the borrowed meshes to their original parents,
            // which has to happen before the frame it's hanging under is destroyed.
            if ((bool)_payloadMount)
            {
                // Unmount explicitly - the item is staying (this is a drone being turned back
                // into TNT), so its meshes have to be put back before the mount goes.
                _payloadMount.Unmount();
                DestroyImmediate(_payloadMount);
                _payloadMount = null;
            }

            SetItemRenderersEnabled(true);
            if ((bool)_frameRoot)
            {
                DestroyImmediate(_frameRoot.gameObject);
                _frameRoot = null;
            }
        }

        private void SetItemRenderersEnabled(bool enabled)
        {
            if (_itemRenderers == null)
            {
                return;
            }
            foreach (Renderer renderer in _itemRenderers)
            {
                if ((bool)renderer)
                {
                    renderer.enabled = enabled;
                }
            }
        }
    }
}
