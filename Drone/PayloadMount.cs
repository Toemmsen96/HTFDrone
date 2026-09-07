using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Slings the payload under the drone as a visible shell.
    ///
    /// Rather than modelling a mortar round, this reuses the payload item's own mesh - it IS the
    /// explosive the drone is carrying, so it always looks like the game's real TNT and follows
    /// whatever payload the drone was built from. The item's renderers normally all get hidden
    /// (the drone shouldn't look like a flying stick of dynamite), so this re-shows them and
    /// parks them under the airframe, lying flat with the fuse trailing behind.
    /// </summary>
    internal class PayloadMount : MonoBehaviour
    {
        // Below the frame, far enough down to read as slung underneath rather than embedded. The
        // drone body is 0.07 tall (so its underside is at -0.035); this clears that with room for
        // the stick's own thickness now that it hangs at full size.
        private static readonly Vector3 MountOffset = new Vector3(0f, -0.09f, 0f);

        // Kept at the item's own size - it's the real TNT, so shrinking it just made it look like
        // a toy hanging off the frame.
        private const float BaseMountScale = 1f;

        // Lay the bundle along the drone's length with the fuse pointing backwards. It stands on
        // its local +Y with the fuse at the top (Explosive animates the fuse burning down local
        // -Y), so pitching +90 tips that top end towards the drone's tail: the sticks end up
        // horizontal, fuse aft.
        private static readonly Quaternion MountRotation = Quaternion.Euler(90f, 0f, 0f);

        private Transform _mount;
        private Renderer[] _payloadRenderers;
        private float _frameScale = 1f;
        private readonly System.Collections.Generic.List<MovedMesh> _moved =
            new System.Collections.Generic.List<MovedMesh>();

        /// <summary>
        /// Slings <paramref name="item"/>'s own mesh under the airframe. <paramref name="frameScale"/>
        /// should match the scale the airframe was built at - it positions the mount relative to
        /// the smaller carried frame. The payload itself always hangs at its true size.
        /// </summary>
        public static PayloadMount Attach(Item item, Renderer[] payloadRenderers, float frameScale = 1f)
        {
            if (!item || payloadRenderers == null || payloadRenderers.Length == 0)
            {
                return null;
            }

            PayloadMount mount = item.gameObject.AddComponent<PayloadMount>();
            mount._payloadRenderers = payloadRenderers;
            mount._frameScale = frameScale;
            return mount;
        }

        /// <summary>The renderers this mount keeps visible, so the drone's hide-everything pass can skip them.</summary>
        public bool IsMountedRenderer(Renderer renderer)
        {
            if (_payloadRenderers == null || !renderer)
            {
                return false;
            }
            foreach (Renderer mounted in _payloadRenderers)
            {
                if (mounted == renderer)
                {
                    return true;
                }
            }
            return false;
        }

        private void Start()
        {
            // Reparent the payload's visual under a mount point of our own, so it hangs beneath
            // the airframe instead of being centred on the item's origin where the quad is.
            _mount = new GameObject("PayloadMount").transform;
            _mount.SetParent(transform, worldPositionStays: false);
            _mount.localPosition = MountOffset * _frameScale;
            _mount.localRotation = MountRotation;

            // Note the payload is NOT scaled by _frameScale: the TNT is a real item and should
            // read at its true size whichever frame it's slung under. Only the mount offset above
            // follows the frame, so it still hangs the right distance below a smaller airframe.
            float scale = BaseMountScale;
            Vector3 parentScale = transform.lossyScale;
            _mount.localScale = new Vector3(
                Mathf.Approximately(parentScale.x, 0f) ? scale : scale / parentScale.x,
                Mathf.Approximately(parentScale.y, 0f) ? scale : scale / parentScale.y,
                Mathf.Approximately(parentScale.z, 0f) ? scale : scale / parentScale.z);

            foreach (Renderer renderer in _payloadRenderers)
            {
                if (!renderer)
                {
                    continue;
                }
                // Only move the actual mesh objects - reparenting anything else on the item would
                // drag colliders or logic components out of position with it.
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    Transform meshTransform = renderer.transform;
                    // Remember where it came from: this is the item's real mesh, so it has to go
                    // back when the drone is unmade, not be left hanging under a deleted mount.
                    _moved.Add(new MovedMesh
                    {
                        Transform = meshTransform,
                        Parent = meshTransform.parent,
                        LocalPosition = meshTransform.localPosition,
                        LocalRotation = meshTransform.localRotation
                    });

                    // Keep each mesh's own offset and rotation. The payload is a *bundle* of
                    // sticks whose arrangement lives in these local transforms - zeroing them
                    // collapsed every stick onto the same point, which is what made it render as
                    // one fat upright cluster instead of a bundle lying flat. Only the mount is
                    // posed; the meshes keep their relative layout underneath it.
                    meshTransform.SetParent(_mount, worldPositionStays: false);
                    renderer.enabled = true;
                }
            }
        }

        /// <summary>
        /// Unmounts the payload and puts the borrowed meshes back where they came from. Call this
        /// when the item is going to KEEP existing (a drone being converted back to TNT); it is
        /// deliberately NOT done from OnDestroy, because when the item itself is being destroyed
        /// (detonation, despawn, scene change) reparenting is both pointless and illegal - Unity
        /// logs "Cannot set the parent of ... while it is being destroyed".
        /// </summary>
        public void Unmount()
        {
            foreach (MovedMesh moved in _moved)
            {
                if (!moved.Transform)
                {
                    continue;
                }
                moved.Transform.SetParent(moved.Parent, worldPositionStays: false);
                moved.Transform.localPosition = moved.LocalPosition;
                moved.Transform.localRotation = moved.LocalRotation;
            }
            _moved.Clear();

            if ((bool)_mount)
            {
                DestroyImmediate(_mount.gameObject);
                _mount = null;
            }
        }

        private struct MovedMesh
        {
            public Transform Transform;
            public Transform Parent;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }
    }
}
