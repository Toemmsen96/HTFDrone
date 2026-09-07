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
                    HeldDroneModel.Remove(item);
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
                    HeldDroneModel.Apply(item);
                }
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
            return (bool)item.Rig && item.Rig.linearVelocity.sqrMagnitude > 4f;
        }
    }
}
