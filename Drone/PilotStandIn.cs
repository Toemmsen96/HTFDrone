using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Shows the pilot's body while they're flying a drone.
    ///
    /// The local player has no third-person model to render: Player.InitializePlayer *destroys*
    /// every object in _otherObjects for the local client, keeping only the first-person hands.
    /// So there's nothing to un-cull - if you fly a drone back at yourself you'd see floating
    /// hands and nothing else.
    ///
    /// The one full body model that does exist locally is the death ragdoll prefab
    /// (GameInfo.DeadPlayerPrefab), which is why you can see yourself when you die. This borrows
    /// that prefab as a stand-in: instantiated locally (never spawned over the network), stripped
    /// of its networking, physics and logic so it's purely a mesh, and parked upright at the
    /// pilot's position for as long as the drone is airborne.
    /// </summary>
    internal class PilotStandIn : MonoBehaviour
    {
        private Player _pilot;
        private Transform _body;

        public static PilotStandIn Create(Player pilot)
        {
            if (!pilot || !DroneState.ShowPilotBody)
            {
                return null;
            }

            GameObject host = new GameObject("DronePilotStandIn");
            PilotStandIn standIn = host.AddComponent<PilotStandIn>();
            standIn._pilot = pilot;
            return standIn;
        }

        private void Start()
        {
            DeadPlayer prefab = GameInfo.DeadPlayerPrefab;
            if (!prefab)
            {
                return;
            }

            // DeadPlayer derives from Item, whose Awake registers colliders in ItemManager's
            // static dictionary and would otherwise leave this decoration sitting in the world's
            // item registry (where the drone's own target finder scans). Instantiating from an
            // inactive parent keeps Awake from running at all, so nothing ever registers - the
            // meshes are then re-shown by hand once the logic has been stripped off.
            GameObject holder = new GameObject("StandInBuilder");
            holder.transform.SetParent(transform, worldPositionStays: false);
            holder.SetActive(false);

            DeadPlayer instance = Instantiate(prefab, holder.transform);
            _body = instance.transform;
            _body.localPosition = Vector3.zero;
            _body.localRotation = Quaternion.identity;

            // Apply the pilot's own hat/outfit/accessory and colours BEFORE stripping - this is
            // the same call the game makes when it spawns your death ragdoll, and it's the reason
            // that ragdoll looks like you. It only assigns meshes and shader colours (no
            // networking), but it lives on DeadPlayer, which the strip below destroys.
            if ((bool)_pilot && (bool)_pilot.Skin)
            {
                _pilot.Skin.SendSkinToDeadPlayer(instance);
            }

            // Strip everything that makes it a ragdoll/network object. What's left is just the
            // meshes - this is a decoration, and must never spawn, sync, collide or fall over.
            Strip(instance.gameObject);

            holder.SetActive(true);
        }

        private static void Strip(GameObject root)
        {
            // Joints FIRST: a CharacterJoint holds a reference to its Rigidbody, and Unity
            // refuses to remove a Rigidbody something still depends on ("Can't remove Rigidbody
            // because CharacterJoint depends on it"). The ragdoll is a whole skeleton of these,
            // so getting this backwards spams one error per bone.
            foreach (Joint joint in root.GetComponentsInChildren<Joint>(includeInactive: true))
            {
                DestroyImmediate(joint);
            }
            foreach (Rigidbody rig in root.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                DestroyImmediate(rig);
            }
            foreach (Collider col in root.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                DestroyImmediate(col);
            }

            // Every behaviour on the prefab is game logic (networking, resurrection, voice chat,
            // slapping) that has no business running on a decoration. Renderers and their
            // transforms aren't MonoBehaviours, so stripping these leaves the model intact.
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                DestroyImmediate(behaviour);
            }
        }

        private void LateUpdate()
        {
            if (!_pilot || !_body)
            {
                return;
            }

            // Stand upright at the pilot, facing where they're looking. The ragdoll prefab's own
            // pose is whatever it was authored in - it isn't animated here, so this reads as the
            // pilot standing still at the controls, which is exactly what they're doing.
            Vector3 lookForward = _pilot.CamObject ? _pilot.CamObject.forward : _pilot.Transform.forward;
            lookForward.y = 0f;
            if (lookForward.sqrMagnitude < 0.001f)
            {
                lookForward = Vector3.forward;
            }

            transform.SetPositionAndRotation(
                _pilot.Transform.position,
                Quaternion.LookRotation(lookForward.normalized, Vector3.up));
        }
    }
}
