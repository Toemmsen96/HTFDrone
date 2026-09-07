using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Picks a homing target for the kamikaze drone: the closest-to-crosshair Creature or other
    /// Player within its FOV cone and max distance. Mirrors the aim-assist logic used by the
    /// aimbot in the sibling HTFCheat mod, just scored on angle-from-forward instead of pure
    /// distance so the drone homes on whatever it's roughly pointed at.
    /// </summary>
    internal static class DroneTargetFinder
    {
        public static Transform FindTarget(Vector3 origin, Vector3 forward, float fov, float maxDistance)
        {
            Transform best = null;
            float bestAngle = fov;

            foreach (Item item in ItemManager.Items.Values)
            {
                Creature creature = item ? item.Creature : null;
                if (!creature || creature.IsDead || !creature.isActiveAndEnabled || creature.IsDeinitializing)
                {
                    continue;
                }
                ConsiderCandidate(GetAimPoint(creature.Rig, creature.transform), creature.transform, origin, forward, maxDistance, ref bestAngle, ref best);
            }

            foreach (Player player in PlayerManager.AlivePlayers)
            {
                if (!player || player == Player.LocalPlayer || player.IsDeinitializing)
                {
                    continue;
                }
                ConsiderCandidate(player.Transform.position, player.Transform, origin, forward, maxDistance, ref bestAngle, ref best);
            }

            return best;
        }

        private static void ConsiderCandidate(Vector3 point, Transform candidate, Vector3 origin, Vector3 forward, float maxDistance, ref float bestAngle, ref Transform best)
        {
            Vector3 toCandidate = point - origin;
            float dist = toCandidate.magnitude;
            if (dist > maxDistance || dist < 0.01f)
            {
                return;
            }

            float angle = Vector3.Angle(forward, toCandidate);
            if (angle > bestAngle)
            {
                return;
            }

            bestAngle = angle;
            best = candidate;
        }

        private static Vector3 GetAimPoint(Rigidbody rig, Transform fallback)
        {
            return rig ? rig.worldCenterOfMass : fallback.position;
        }
    }
}
