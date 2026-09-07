using System;
using HarmonyLib;
using UnityEngine;

using HTFDrone.Drone;

namespace HTFDrone
{
    internal class Patches
    {

        // [HarmonyPatch(typeof(Application), "isEditor", MethodType.Getter)]
        // [HarmonyPostfix]
        // private static void EnableEditor(ref bool __result)
        // {
        //     __result = true; // Force the game to think it's running in the editor, this is a basic standard patch.
        //     Plugin.logger.LogInfo("Editor enabled");
        // }

        // Freezes the local player's body/camera/hands while piloting the FPV drone - the same
        // switch the game itself uses for pausing, dying, driving a self-driving boat etc.
        // (Player.BlockInputs is a read-only computed property with no setter, so a Harmony
        // postfix is the only way to force it without touching the game's own fields.)
        [HarmonyPatch(typeof(Player), "BlockInputs", MethodType.Getter)]
        [HarmonyPostfix]
        private static void BlockInputsWhilePilotingDrone(Player __instance, ref bool __result)
        {
            if (!__result && __instance == Player.LocalPlayer && DroneState.DroneInFlight)
            {
                __result = true;
            }
        }
    }
}
