using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Builds a visible quadcopter out of Unity primitives at runtime (body, four arms, four
    /// motor pods and spinning props) and parents it to the flying item, so the drone is an
    /// actual craft you can see rather than an invisible dynamite stick. No asset bundle or
    /// authored prefab needed - everything here is generated from primitive meshes and a couple
    /// of runtime materials, which keeps the mod a single self-contained DLL.
    ///
    /// It also defines where the "nose" is: DroneState.CameraForwardMargin is measured from this
    /// frame, and is set to land on the camera pod built below - behind and under the prop disc,
    /// so the pilot sees prop tips sweeping the top of frame like a real FPV feed.
    /// </summary>
    internal class DroneModel : MonoBehaviour
    {
        private const float ArmLength = 0.28f;
        private const float PropRadius = 0.16f;

        // How far above the frame the prop disc sits. It has to clear the camera pod (which tops
        // out around y=0.07) or the front props would be hidden behind it from the lens's own
        // position - the whole point being that the pilot sees them sweeping the top of frame.
        private const float PropHeight = 0.075f;

        private Transform _frameRoot;
        private Transform[] _props;
        private float _propSpin;
        private bool _visible = true;

        public static DroneModel Attach(GameObject droneObject)
        {
            DroneModel model = droneObject.GetComponent<DroneModel>();
            if ((bool)model)
            {
                return model;
            }
            return droneObject.AddComponent<DroneModel>();
        }

        /// <summary>
        /// Shows/hides the airframe. A real nose-mounted FPV cam can't see its own frame, so the
        /// pilot flying it hides the model while everyone else still watches it fly past.
        /// </summary>
        public void SetVisible(bool visible)
        {
            // Remembered so a call made before Start() built the frame still takes effect.
            _visible = visible;
            if (!_frameRoot)
            {
                return;
            }
            foreach (Renderer renderer in _frameRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = visible;
            }
        }

        /// <summary>
        /// Builds the airframe under <paramref name="parent"/> and returns its root, filling
        /// <paramref name="props"/> with the four spinning propellers. Shared so the flying drone
        /// and the one you carry in your hands are visibly the same craft, built from one
        /// definition rather than two that can drift apart.
        /// </summary>
        public static Transform BuildFrame(Transform parent, float scale, out Transform[] props)
        {
            Transform root = new GameObject("DroneFrame").transform;
            root.SetParent(parent, worldPositionStays: false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            // Item prefabs carry their own root scale, which would otherwise shrink or stretch
            // the whole airframe - cancel it out so the sizes below are real metres.
            Vector3 parentScale = parent.lossyScale;
            root.localScale = new Vector3(
                Mathf.Approximately(parentScale.x, 0f) ? scale : scale / parentScale.x,
                Mathf.Approximately(parentScale.y, 0f) ? scale : scale / parentScale.y,
                Mathf.Approximately(parentScale.z, 0f) ? scale : scale / parentScale.z);

            Material bodyMat = CreateMaterial(new Color(0.09f, 0.09f, 0.11f));
            Material accentMat = CreateMaterial(new Color(0.85f, 0.15f, 0.1f));
            Material propMat = CreateMaterial(new Color(0.2f, 0.2f, 0.22f, 0.65f));

            // Main body - a flattened box, nose pointing along local +Z.
            Transform body = CreatePrimitive(PrimitiveType.Cube, root, "Body", bodyMat);
            body.localPosition = Vector3.zero;
            body.localScale = new Vector3(0.16f, 0.07f, 0.30f);

            // Camera pod up front, angled up slightly like a real FPV cam.
            Transform camPod = CreatePrimitive(PrimitiveType.Cube, root, "CameraPod", accentMat);
            camPod.localPosition = new Vector3(0f, 0.035f, 0.14f);
            camPod.localScale = new Vector3(0.075f, 0.075f, 0.075f);
            camPod.localRotation = Quaternion.Euler(-20f, 0f, 0f);

            // Battery strapped on top.
            Transform battery = CreatePrimitive(PrimitiveType.Cube, root, "Battery", accentMat);
            battery.localPosition = new Vector3(0f, 0.055f, -0.04f);
            battery.localScale = new Vector3(0.10f, 0.045f, 0.16f);

            props = new Transform[4];
            int propIndex = 0;
            for (int xSign = -1; xSign <= 1; xSign += 2)
            {
                for (int zSign = -1; zSign <= 1; zSign += 2)
                {
                    Vector3 armEnd = new Vector3(xSign * ArmLength * 0.7f, 0f, zSign * ArmLength * 0.7f);

                    Transform arm = CreatePrimitive(PrimitiveType.Cube, root, "Arm", bodyMat);
                    arm.localPosition = armEnd * 0.5f;
                    arm.localRotation = Quaternion.LookRotation(armEnd.normalized, Vector3.up);
                    arm.localScale = new Vector3(0.035f, 0.025f, armEnd.magnitude);

                    Transform motor = CreatePrimitive(PrimitiveType.Cylinder, root, "Motor", bodyMat);
                    motor.localPosition = armEnd + Vector3.up * 0.02f;
                    motor.localScale = new Vector3(0.05f, 0.025f, 0.05f);

                    // The prop is a hub with two crossed blades rather than one bar. A single
                    // bar spinning at 2400 deg/s is edge-on to the FPV lens half the time and
                    // strobes against the frame rate; a cross keeps something in frame at every
                    // rotation angle, which is what makes it read as a blurred disc.
                    Transform prop = new GameObject("Prop").transform;
                    prop.SetParent(root, worldPositionStays: false);
                    prop.localPosition = armEnd + Vector3.up * PropHeight;
                    prop.localRotation = Quaternion.identity;

                    for (int blade = 0; blade < 2; blade++)
                    {
                        Transform bladeT = CreatePrimitive(PrimitiveType.Cube, prop, "Blade", propMat);
                        bladeT.localPosition = Vector3.zero;
                        bladeT.localRotation = Quaternion.Euler(0f, blade * 90f, 0f);
                        bladeT.localScale = new Vector3(PropRadius * 2f, 0.006f, 0.022f);
                    }

                    props[propIndex++] = prop;
                }
            }

            return root;
        }

        /// <summary>Spins the props. Shared so a carried drone idles the same way a flying one does.</summary>
        public static void SpinProps(Transform[] props, ref float spin, float degreesPerSecond)
        {
            if (props == null)
            {
                return;
            }
            spin += degreesPerSecond * Time.deltaTime;
            for (int i = 0; i < props.Length; i++)
            {
                if (!props[i])
                {
                    continue;
                }
                // Alternate direction per motor, like a real quad's counter-rotating pairs.
                float dir = (i % 2 == 0) ? 1f : -1f;
                props[i].localRotation = Quaternion.Euler(0f, spin * dir, 0f);
            }
        }

        private void Start()
        {
            _frameRoot = BuildFrame(transform, 1f, out _props);

            // Apply any visibility set before the frame existed.
            SetVisible(_visible);
        }

        private void Update()
        {
            // Props always spin fast enough to blur - real quads idle high, and a frozen prop
            // reads as "broken" more than "low throttle".
            SpinProps(_props, ref _propSpin, 2400f);
        }

        private void OnDestroy()
        {
            // The frame is a child GameObject, not part of this component - it has to be torn
            // down explicitly or an invisible airframe keeps flying after the drone is gone.
            if ((bool)_frameRoot)
            {
                Destroy(_frameRoot.gameObject);
            }
        }

        private static Transform CreatePrimitive(PrimitiveType type, Transform parent, string name, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            // Primitives come with colliders attached - the item's own colliders already handle
            // physics, and these would fight them (and collide with the drone itself).
            Collider col = obj.GetComponent<Collider>();
            if ((bool)col)
            {
                Destroy(col);
            }
            obj.transform.SetParent(parent, worldPositionStays: false);
            Renderer renderer = obj.GetComponent<Renderer>();
            if ((bool)renderer)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return obj.transform;
        }

        private static Material CreateMaterial(Color color)
        {
            // URP's lit shader is what the game renders with; fall back to the built-in one if
            // it can't be found. Note the explicit bool checks rather than ?? - Unity overloads
            // == for Object, and ?? bypasses that overload, so a destroyed/missing shader would
            // sneak through as non-null.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader)
            {
                shader = Shader.Find("Standard");
            }
            if (!shader)
            {
                shader = Shader.Find("Sprites/Default");
            }
            Material mat = new Material(shader);
            mat.color = color;
            return mat;
        }
    }
}
