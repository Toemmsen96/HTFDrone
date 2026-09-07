using System.Collections.Generic;
using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Shows other players' drones as drones.
    ///
    /// The flight logic only ever runs on the pilot's machine - the item's movement reaches
    /// everyone else through the game's own RigidbodySync, which streams position and rotation
    /// from whoever is simulating it. That's what makes this mod work in multiplayer without any
    /// custom networking, but it also means a remote player would otherwise just see a stick of
    /// TNT flying past. This watches for explosive items being simulated by someone else and
    /// dresses them in the drone airframe locally.
    ///
    /// Players without the mod are unaffected: they see a flying TNT and take normal explosion
    /// damage, so the game stays consistent for them - they just miss the cosmetics.
    /// </summary>
    internal class RemoteDroneWatcher : MonoBehaviour
    {
        // Airborne for this long while simulated by a remote player before we call it a drone.
        // Thrown TNT arcs and lands quickly; a drone keeps flying, so a short delay separates the
        // two without needing to send anything over the network.
        private const float FlightTimeBeforeDressing = 1.2f;

        private const float ScanInterval = 0.5f;

        private static RemoteDroneWatcher _instance;

        private readonly Dictionary<Item, float> _airborneSince = new Dictionary<Item, float>();
        private readonly List<Item> _stale = new List<Item>();
        private float _nextScan;

        public static void Ensure(GameObject host)
        {
            if (_instance == null)
            {
                _instance = host.AddComponent<RemoteDroneWatcher>();
            }
        }

        private void Update()
        {
            if (!DroneState.ShowOtherPlayersDrones || Time.time < _nextScan)
            {
                return;
            }
            _nextScan = Time.time + ScanInterval;

            _stale.Clear();
            foreach (KeyValuePair<Item, float> tracked in _airborneSince)
            {
                if (!tracked.Key || !IsRemotelyFlown(tracked.Key))
                {
                    _stale.Add(tracked.Key);
                }
            }
            foreach (Item item in _stale)
            {
                _airborneSince.Remove(item);
                if ((bool)item)
                {
                    Undress(item);
                }
            }

            foreach (Item item in ItemManager.Items.Values)
            {
                if (!item || !IsRemotelyFlown(item))
                {
                    continue;
                }

                if (!_airborneSince.TryGetValue(item, out float since))
                {
                    _airborneSince[item] = Time.time;
                    continue;
                }

                if (Time.time - since >= FlightTimeBeforeDressing)
                {
                    Dress(item);
                }
            }
        }

        /// <summary>
        /// Dresses a remote drone in the full-size flying airframe - the same DroneModel the
        /// pilot's own craft uses, not the smaller HeldDroneModel meant for a drone in the hand.
        /// A remote drone is in flight, so it should read at the same size and prop speed as one
        /// flown locally; dressing it as a carried drone made it a half-size craft idling its
        /// props while tearing past.
        /// </summary>
        private static void Dress(Item item)
        {
            if (!item || (bool)item.GetComponent<DroneModel>())
            {
                return;
            }

            // Snapshot the payload's own renderers before the airframe exists, so the mount slings
            // the TNT rather than the drone parts we're about to add.
            Renderer[] payloadRenderers = item.GetComponentsInChildren<Renderer>(includeInactive: true);

            DroneModel.Attach(item.gameObject);
            PayloadMount.Attach(item, payloadRenderers);
        }

        /// <summary>Strips the airframe back off when an item stops being a flying remote drone.</summary>
        private static void Undress(Item item)
        {
            if (!item)
            {
                return;
            }

            PayloadMount mount = item.GetComponent<PayloadMount>();
            if ((bool)mount)
            {
                // Put the borrowed TNT meshes back before the frame they hang under goes away.
                mount.Unmount();
                Destroy(mount);
            }

            DroneModel model = item.GetComponent<DroneModel>();
            if ((bool)model)
            {
                Destroy(model);
            }
        }

        /// <summary>
        /// True for an explosive that some other player is simulating and that is actually flying.
        /// _syncedSimulator is a replicated SyncVar, so every client can see who owns an item's
        /// physics - no extra messages needed to work out that someone else is flying something.
        /// </summary>
        private static bool IsRemotelyFlown(Item item)
        {
            if (!item || item.IsDeinitializing || !DronePayload.HasExplosive(item))
            {
                return false;
            }
            if ((bool)item.SyncedHolder)
            {
                return false; // Being carried, not flying.
            }

            RigidbodySync sync = item.RigidbodySync;
            if (!sync || sync.IsSimulatedLocal)
            {
                return false; // Ours - KamikazeDrone already handles the local case.
            }
            if (sync.SyncedSimulator == null || !sync.SyncedSimulator.IsActive)
            {
                return false; // Nobody is driving it.
            }

            // Airborne and moving, rather than sat on the ground where a dropped TNT would be.
            //
            // Read from FakeVelocity, NOT Rig.linearVelocity. An item simulated by someone else
            // is kinematic here and RigidbodySync.FixedUpdate calls ZeroVelocity() on it every
            // tick - its rigidbody velocity is always zero no matter how fast it's actually
            // travelling, because the motion arrives as transform writes from the network rather
            // than as physics. FakeVelocity is the game's own reconstruction of speed from those
            // transform deltas, which is the only meaningful speed a remote item has.
            return sync.FakeVelocity.sqrMagnitude > 4f;
        }
    }
}
