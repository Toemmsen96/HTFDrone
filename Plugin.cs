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
        private const string modVersion = "1.1.0";
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
            
            // Registered one at a time: RegisterCommand binds a BepInEx config key derived from
            // the command's Name, and BepInEx throws on characters like ' " = [ ]. Letting that
            // escape would abort the rest of Awake - which is how a single apostrophe in one
            // command name previously took the scene hook and remote watcher below down with it.
            foreach (CustomCommand command in Commands.Commands.AllCommands)
            {
                try
                {
                    CTDynamicModMenu.CTDynamicModMenu.Instance.RegisterCommand(command);
                }
                catch (System.Exception e)
                {
                    logger.LogError($"Failed to register command '{command.Name}': {e.Message}");
                }
            }

            // Islands are additively loaded scenes, so this fires once per island - the hook for
            // adding a drone stand to each one. (Patching IslandManager's load coroutine instead
            // would run per MoveNext and stack up duplicate stands.)
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            // Watches for drones flown by other modded players, so they look like drones here too.
            // Local and cosmetic - see RemoteDroneWatcher for why this needs no custom networking.
            HTFDrone.Drone.RemoteDroneWatcher.Ensure(gameObject);

            logger.LogInfo(modGUID+" loaded");
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            StartCoroutine(AddDroneStandNextFrame());
        }

        private System.Collections.IEnumerator AddDroneStandNextFrame()
        {
            // Wait for the scene's own objects to run Awake/Start before cloning one of them -
            // an ItemPurchasable registers its interact collider in Awake, and cloning it before
            // that has happened gives a stand that can't be interacted with.
            yield return null;
            yield return new UnityEngine.WaitForFixedUpdate();

            try
            {
                HTFDrone.Drone.DroneShop.AddStandToCurrentIsland();
            }
            catch (System.Exception e)
            {
                logger.LogError($"Failed to add drone stand: {e}");
            }
        }
    }
}