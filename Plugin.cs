using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using CTDynamicModMenu.Commands;


namespace HTFDrone
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("Toemmsen96.CTDynamicModMenu")]
    public partial class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "toemmsen.HTFDrone";
        private const string modName = "HTFDrone";
        private const string modVersion = "1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        internal static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        private static Plugin instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            harmony.PatchAll(typeof(Patches));
            harmony.PatchAll(typeof(Plugin));
            
            foreach (CustomCommand command in Commands.Commands.AllCommands)
            {
                CTDynamicModMenu.CTDynamicModMenu.Instance.RegisterCommand(command);
            }
            logger.LogInfo(modGUID+" loaded");
        }
    }
}