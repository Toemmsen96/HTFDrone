using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HTFDrone.Drone
{
    /// <summary>
    /// Makes the drone a buyable item. There is no separate drone prefab - the drone is the
    /// game's explosive item plus the flight components, so instead we clone an existing shop
    /// stand on every island, point it at the payload item, and remember which purchased item
    /// instances came from a drone stand. Throwing one of those launches it as a drone.
    /// </summary>
    internal static class DroneShop
    {
        // Item instances bought from a drone stand.
        private static readonly HashSet<Item> _droneItems = new HashSet<Item>();

        // Set for the brief window between pressing buy and the server handing the item over, so
        // the newly spawned item can be recognised as the one that was just purchased.
        private static float _pendingPurchaseTime = -1f;
        private static byte _pendingPurchaseId;
        private static bool _launchOnArrival;

        private static readonly FieldInfo ItemToPurchaseField =
            AccessTools.Field(typeof(ItemPurchasable), "_itemToPurchase");
        private static readonly FieldInfo CustomCostField =
            AccessTools.Field(typeof(Purchasable), "_customCost");
        private static readonly FieldInfo ModelsToOutlineField =
            AccessTools.Field(typeof(Interactable), "_modelsToOutline");

        public static bool IsDroneItem(Item item)
        {
            return (bool)item && _droneItems.Contains(item);
        }

        public static void MarkAsDrone(Item item)
        {
            if ((bool)item)
            {
                // Destroyed items compare equal to null under Unity's == overload but still sit
                // in the set, so clear those out as we go rather than leaking entries.
                _droneItems.RemoveWhere(existing => !existing);
                _droneItems.Add(item);
            }
        }

        public static void ClearDroneItem(Item item)
        {
            if ((bool)item)
            {
                _droneItems.Remove(item);
                HeldDroneModel.Remove(item);
            }
        }

        /// <summary>
        /// Converts a held TNT into a drone, or back again. Nothing is swapped or respawned - a
        /// drone IS the explosive item, so this is just a tag plus the visible airframe. That
        /// keeps it entirely client-side and avoids any network item-swap the game has no RPC for.
        /// </summary>
        public static void ToggleDroneConversion(Item item)
        {
            if (!item)
            {
                return;
            }

            if (IsDroneItem(item))
            {
                ClearDroneItem(item);
                CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Drone packed away - back to TNT.");
                return;
            }

            MarkAsDrone(item);
            HeldDroneModel.Apply(item);
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Drone assembled - throw it to launch.");
        }

        /// <summary>
        /// Set while the player is hovering a drone stand, so the hover-text rewrite below only
        /// touches that stand's label and leaves every other TNT in the world alone.
        /// </summary>
        public static bool IsHoveringDroneStand;

        /// <summary>
        /// Set while a drone stand is hiding its label from UnHover. Separate from the hover flag
        /// because UnHover runs outside the Hover call, but the rewrite has to apply to both -
        /// see RewriteHoverText.
        /// </summary>
        public static bool IsUnhoveringDroneStand;

        /// <summary>
        /// Swaps the payload item's name out of a drone stand's hover text. The stand points at
        /// the shared prefab rather than a tagged instance, so the Item.GetName patch can't cover
        /// it - the text has to be fixed up on its way to the UI instead.
        /// </summary>
        public static string RewriteHoverText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // The window this runs in has to cover the hide as well as the show. The flag is set
            // inside ItemPurchasable.Hover, but PlayerUI.HideLookAtText is called from UnHover,
            // outside it - so the hide used to be handed the original "TNT" string while the
            // label on screen said "Drone". HideLookAtText only hides when the two match exactly,
            // so the label was never hidden: it stayed up after looking away, and every fresh
            // hover stacked another scale tween on it until the price ballooned off the screen.
            if (!IsHoveringDroneStand && !IsUnhoveringDroneStand)
            {
                return text;
            }

            Item payload = DronePayload.Resolve();
            if (!payload)
            {
                return text;
            }

            string payloadName = payload.GetName();
            if (string.IsNullOrEmpty(payloadName) || payloadName == DroneState.DroneItemName)
            {
                return text;
            }

            return text.Replace(payloadName, DroneState.DroneItemName);
        }

        /// <summary>
        /// Forces the drone price onto a stand. ItemPurchasable.Hover recomputes _customCost from
        /// the payload item's Cost every hover frame, so setting it once at clone time isn't
        /// enough - without this the stand quietly charges the payload's price instead.
        /// </summary>
        public static void ApplyDronePrice(ItemPurchasable stand)
        {
            if ((bool)stand)
            {
                CustomCostField?.SetValue(stand, DroneState.DronePrice);
            }
        }

        /// <summary>Called when a drone stand is interacted with, just before the server spawns the item.</summary>
        public static void NotePendingPurchase(byte itemId)
        {
            _pendingPurchaseTime = Time.time;
            _pendingPurchaseId = itemId;
            _launchOnArrival = false;
        }

        /// <summary>
        /// As NotePendingPurchase, but the item is launched as a drone the moment it arrives
        /// instead of being handed over to carry. Used by /fpvdrone on a client, where the item
        /// has to come back from the server rather than being spawned locally.
        /// </summary>
        public static void RequestLaunchOnArrival(byte itemId)
        {
            _pendingPurchaseTime = Time.time;
            _pendingPurchaseId = itemId;
            _launchOnArrival = true;
        }

        /// <summary>
        /// Called as items are handed to the local player. Purchases arrive via a server RPC, so
        /// they land a round trip after the button press rather than synchronously - we match on
        /// item id within a short window instead of being able to tag the instance directly.
        /// </summary>
        public static void TryClaimPendingPurchase(Item item)
        {
            if (!item || _pendingPurchaseTime < 0f)
            {
                return;
            }
            if (Time.time - _pendingPurchaseTime > 5f || item.ID != _pendingPurchaseId)
            {
                return;
            }

            _pendingPurchaseTime = -1f;

            if (_launchOnArrival)
            {
                _launchOnArrival = false;
                if (DroneLauncher.LaunchExisting(item, Player.LocalPlayer))
                {
                    CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Drone away!");
                }
                return;
            }

            MarkAsDrone(item);
            HeldDroneModel.Apply(item);
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage("Drone acquired - throw it to launch.");
        }

        /// <summary>
        /// Adds a drone stand to whichever island just loaded, by cloning a stand that's already
        /// there. Cloning rather than building one from scratch keeps all the private serialized
        /// wiring an Interactable needs (outline models, interact collider, text target) intact.
        /// </summary>
        public static void AddStandToCurrentIsland()
        {
            Item payload = DronePayload.Resolve();
            if (!payload)
            {
                return;
            }

            ItemPurchasable template = FindTemplateStand();
            if (!template)
            {
                // Island has no shop at all - nothing to clone, so nothing to do.
                return;
            }

            ItemPurchasable clone = Object.Instantiate(
                template,
                template.transform.position + template.transform.right * 1.6f,
                template.transform.rotation,
                template.transform.parent);
            clone.name = "DronePurchasable";

            ItemToPurchaseField?.SetValue(clone, payload);
            CustomCostField?.SetValue(clone, DroneState.DronePrice);
            DressStandAsDrone(clone);

            Plugin.logger.LogInfo("Added drone stand next to " + template.name);
        }

        /// <summary>
        /// Makes the cloned stand actually look like it sells drones. The clone inherits whatever
        /// the original was displaying (a gun, bait, whatever), so the displayed item's mesh is
        /// hidden and a drone mounted in its place. The outline models array is left pointing at
        /// the same objects, so hover highlighting still works - it's only what they render that
        /// changes.
        /// </summary>
        private static void DressStandAsDrone(ItemPurchasable stand)
        {
            GameObject[] outlineModels = ModelsToOutlineField?.GetValue(stand) as GameObject[];
            if (outlineModels == null || outlineModels.Length == 0)
            {
                return;
            }

            GameObject display = outlineModels[0];
            if (!display)
            {
                return;
            }

            foreach (Renderer renderer in display.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = false;
            }

            // Slowly turning on the spot, the way shop display models usually are presented.
            StandDroneDisplay.Attach(display.transform);
        }

        private static ItemPurchasable FindTemplateStand()
        {
            ItemPurchasable[] stands = Object.FindObjectsByType<ItemPurchasable>(FindObjectsInactive.Exclude);
            foreach (ItemPurchasable stand in stands)
            {
                // Skip our own clones, or each island reload would stack another copy on top.
                if (stand.name != "DronePurchasable")
                {
                    return stand;
                }
            }
            return null;
        }
    }
}
