using System.Reflection;
using System.Text;
using CTDynamicModMenu.Commands;
using HarmonyLib;
using UnityEngine;

namespace HTFDrone.Commands
{
    /// <summary>
    /// Dumps the drone stand's object hierarchy with live world scales, for tracking down display
    /// problems that only show up in the running game (the stand is a clone of a scene object, so
    /// its wiring isn't visible from the decompiled source).
    /// </summary>
    internal class DroneStandDiag : CustomCommand
    {
        public override string Name => "Drone Stand Diagnostics";
        public override string Description => "Dumps the drone stand's hierarchy and live scales.";
        public override string Format => "/dronestanddiag";
        public override string Category => "Drone";

        private static readonly FieldInfo ModelsToOutlineField =
            AccessTools.Field(typeof(Interactable), "_modelsToOutline");
        private static readonly FieldInfo TextTargetField =
            AccessTools.Field(typeof(Interactable), "_textTarget");
        private static readonly FieldInfo CustomCostField =
            AccessTools.Field(typeof(Purchasable), "_customCost");

        public override void Execute(CommandInput message)
        {
            StringBuilder sb = new StringBuilder();
            ItemPurchasable[] stands = Object.FindObjectsByType<ItemPurchasable>(FindObjectsInactive.Include);

            foreach (ItemPurchasable stand in stands)
            {
                if (stand.name != "DronePurchasable")
                {
                    continue;
                }

                sb.AppendLine($"stand '{stand.name}' cost={CustomCostField?.GetValue(stand)}");

                Transform textTarget = TextTargetField?.GetValue(stand) as Transform;
                sb.AppendLine($"  textTarget: {(textTarget ? textTarget.name : "<null>")} " +
                              $"lossy={(textTarget ? textTarget.lossyScale.ToString("0.###") : "-")}");

                if (ModelsToOutlineField?.GetValue(stand) is GameObject[] models)
                {
                    for (int i = 0; i < models.Length; i++)
                    {
                        GameObject m = models[i];
                        sb.AppendLine($"  outline[{i}]: {(m ? m.name : "<null>")} " +
                                      $"lossy={(m ? m.transform.lossyScale.ToString("0.###") : "-")}");
                    }
                }

                Dump(stand.transform, sb, 1);
                sb.AppendLine();
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("No drone stand found on this island.");
            }

            Plugin.logger.LogInfo(sb.ToString());
            CTDynamicModMenu.CTDynamicModMenu.Instance.DisplayMessage(sb.ToString());
        }

        private static void Dump(Transform t, StringBuilder sb, int depth)
        {
            if (depth > 4)
            {
                return;
            }
            string pad = new string(' ', depth * 2);
            foreach (Transform child in t)
            {
                Component text = child.GetComponent("TMPro.TextMeshProUGUI") ?? child.GetComponent("TMPro.TextMeshPro");
                sb.AppendLine($"{pad}{child.name} local={child.localScale.ToString("0.###")} " +
                              $"lossy={child.lossyScale.ToString("0.###")}{(text ? "  <TEXT>" : "")}");
                Dump(child, sb, depth + 1);
            }
        }
    }
}
