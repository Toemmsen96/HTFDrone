using System;
using HarmonyLib;
using UnityEngine;

using HTFDrone.Drone;

namespace HTFDrone
{
    internal class Patches
    {
        // Debounce for the drone conversion key (see ConvertHeldItemToDrone).
        private static float _lastConvertTime;

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

        // Note the moment a drone stand is used. The item itself arrives later (the server spawns
        // it via RPC), so DroneShop matches it up when it lands in the player's hands.
        [HarmonyPatch(typeof(ItemPurchasable), nameof(ItemPurchasable.Interact))]
        [HarmonyPrefix]
        private static void NoteDronePurchase(ItemPurchasable __instance)
        {
            if (__instance.name != "DronePurchasable")
            {
                return;
            }
            Item payload = DronePayload.Resolve();
            if ((bool)payload)
            {
                DroneShop.NotePendingPurchase(payload.ID);
            }
        }

        // Catch the purchased item as it's handed over, so it can be tagged as a drone.
        [HarmonyPatch(typeof(PlayerHolding), nameof(PlayerHolding.SetHeldItem))]
        [HarmonyPostfix]
        private static void ClaimPurchasedDrone(PlayerHolding __instance, Item item)
        {
            if ((bool)item && (bool)Player.LocalPlayer && __instance == Player.LocalPlayer.Holding)
            {
                DroneShop.TryClaimPendingPurchase(item);
            }
        }

        // A drone is the game's explosive item with a tag on it, so left alone it would still call
        // itself TNT everywhere - in your hands, on hover, and on the shop stand. Renaming it here
        // covers all of those at once, since they all build their text from GetName().
        [HarmonyPatch(typeof(Item), nameof(Item.GetName))]
        [HarmonyPostfix]
        private static void RenameDroneItem(Item __instance, ref string __result)
        {
            if (DroneShop.IsDroneItem(__instance))
            {
                __result = DroneState.DroneItemName;
            }
        }

        // The stand points at the shared payload prefab, which isn't a tagged drone instance, so
        // the GetName patch above doesn't cover it - the stand's hover text would still say TNT.
        // Catching the text on its way into the UI covers both the initial set and the per-frame
        // update, without having to track the stand's private _hoverString.
        // Flag the drone stand's own hover pass, so the text rewrite below only applies to its
        // label rather than to every TNT the player looks at.
        [HarmonyPatch(typeof(ItemPurchasable), nameof(ItemPurchasable.Hover))]
        [HarmonyPrefix]
        private static void MarkDroneStandHoverStart(ItemPurchasable __instance)
        {
            DroneShop.IsHoveringDroneStand = __instance.name == "DronePurchasable";
        }

        [HarmonyPatch(typeof(ItemPurchasable), nameof(ItemPurchasable.Hover))]
        [HarmonyPostfix]
        private static void MarkDroneStandHoverEnd(ItemPurchasable __instance)
        {
            // ItemPurchasable.Hover rebuilds _customCost from the payload item's own Cost on
            // every single hover frame, which silently undid the drone price set when the stand
            // was cloned - the stand looked right but charged (and gated affordability on) the
            // price of a stick of TNT. Reapply it after the game has had its say.
            if (DroneShop.IsHoveringDroneStand)
            {
                DroneShop.ApplyDronePrice(__instance);
            }
            DroneShop.IsHoveringDroneStand = false;
        }

        // The label is hidden from UnHover, outside the Hover window the flag above covers. The
        // hide is matched against the *displayed* string, so the rewrite has to apply here too or
        // the game asks to hide "TNT" while the label reads "Drone" and it never goes away.
        // Patched on Purchasable, not ItemPurchasable: UnHover is declared on the base and the
        // subclass doesn't override it, so naming the subclass would fail to resolve and take
        // every other patch in this class down with it at PatchAll time.
        [HarmonyPatch(typeof(Purchasable), nameof(Purchasable.UnHover))]
        [HarmonyPrefix]
        private static void MarkDroneStandUnhoverStart(Purchasable __instance)
        {
            DroneShop.IsUnhoveringDroneStand = __instance.name == "DronePurchasable";
        }

        [HarmonyPatch(typeof(Purchasable), nameof(Purchasable.UnHover))]
        [HarmonyPostfix]
        private static void MarkDroneStandUnhoverEnd()
        {
            DroneShop.IsUnhoveringDroneStand = false;
        }

        [HarmonyPatch(typeof(PlayerUI), nameof(PlayerUI.SetLookAtText))]
        [HarmonyPrefix]
        private static void RenameDroneStandLookAtText(ref string text)
        {
            text = DroneShop.RewriteHoverText(text);
        }

        [HarmonyPatch(typeof(PlayerUI), nameof(PlayerUI.UpdateLookAtText))]
        [HarmonyPrefix]
        private static void RenameDroneStandUpdateText(ref string text)
        {
            text = DroneShop.RewriteHoverText(text);
        }

        // The hide path matters as much as the show path: ItemUI.HideLookAtText only hides the
        // label when the string it's given matches the one currently displayed, so this has to
        // receive the same rewrite the label was shown with.
        [HarmonyPatch(typeof(PlayerUI), nameof(PlayerUI.HideLookAtText))]
        [HarmonyPrefix]
        private static void RenameDroneStandHideText(ref string text)
        {
            text = DroneShop.RewriteHoverText(text);
        }

        // Convert a held TNT into a drone (and back) with the skin-swap key.
        //
        // Y/C are already bound by the game to the "PlayerChangeSkin" action (that's the weapon
        // skin swap), so reading those keys directly just fought the existing binding. Riding the
        // game's own action instead means it fires on exactly the key the player has bound, and
        // nothing is lost by taking it over for explosives - they have no skins to cycle.
        [HarmonyPatch(typeof(PlayerHolding), "ChangeSkinInput")]
        [HarmonyPrefix]
        private static bool ConvertHeldItemToDrone(PlayerHolding __instance)
        {
            if (!Player.LocalPlayer || __instance != Player.LocalPlayer.Holding)
            {
                return true;
            }
            if (Player.LocalPlayer.BlockInputs)
            {
                return true;
            }

            Item held = __instance.HeldItem;
            if (!held || !DronePayload.HasExplosive(held))
            {
                return true; // Not an explosive - let the normal skin swap run.
            }

            // Debounce: the action is an axis (Y and C are opposite directions of the same
            // binding), so guard against a single press toggling more than once.
            if (Time.time - _lastConvertTime < 0.25f)
            {
                return false;
            }
            _lastConvertTime = Time.time;

            DroneShop.ToggleDroneConversion(held);
            return false; // Handled - skip the skin swap for this press.
        }

        // Throwing (or dropping) a bought drone is what launches it.
        [HarmonyPatch(typeof(PlayerHolding), nameof(PlayerHolding.DropItem))]
        [HarmonyPrefix]
        private static void LaunchDroneOnThrow(PlayerHolding __instance, bool calledFromLocal, Item droppedItem)
        {
            if (!calledFromLocal || !Player.LocalPlayer || __instance != Player.LocalPlayer.Holding)
            {
                return;
            }

            // DropItem falls back to the currently held item when droppedItem is null - mirror
            // that here so a plain throw is caught as well as an explicit drop.
            Item item = (bool)droppedItem ? droppedItem : Player.LocalPlayer.Holding.HeldItem;
            if (!DroneShop.IsDroneItem(item))
            {
                return;
            }

            // Runs as a prefix so the item is still held: the launch reads the player's camera to
            // decide which way the drone sets off.
            DroneShop.ClearDroneItem(item);
            DroneLauncher.LaunchExisting(item, Player.LocalPlayer);
        }

    }
}
