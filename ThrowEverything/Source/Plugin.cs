using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ThrowEverything.Models;
using ThrowEverything.Patches;

namespace ThrowEverything
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalCompanyInputUtils.LethalCompanyInputUtilsPlugin.ModId, BepInDependency.DependencyFlags.HardDependency)]

    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);

        internal static new BepInEx.Logging.ManualLogSource Logger;
        internal static ConfigEntry<bool> IgnoreWeightSetting;
        internal static ConfigEntry<bool> IgnoreStaminaSetting;

        private void Awake()
        {
            Logger = base.Logger;
            InitSettings();
            harmony.PatchAll(typeof(GrabbableObject_Patch));
            harmony.PatchAll(typeof(HUDManager_Patch));
            harmony.PatchAll(typeof(PlayerControllerB_Patch));
            Throwable.HookEvents();

            if (InputSettings.Instance.Enabled)
            {
                Logger.LogInfo($"InputUtils is working");
            }

            Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} is loaded!");
        }


        private void InitSettings()
        {

            IgnoreStaminaSetting = Config.Bind("Settings", "Ignore Stamina", false, "Whether to ignore stamina when throwing objects");
            IgnoreWeightSetting = Config.Bind("Settings", "Ignore Weight", false, "Whether to ignore weight when throwing objects");
            IgnoreStaminaSetting.SettingChanged += (obj, args) =>
            {
                IgnoreStamina = IgnoreStaminaSetting.Value;
                Logger.LogInfo($"Ignore Stamina: {IgnoreStamina}");
            };
            IgnoreWeightSetting.SettingChanged += (obj, args) =>
            {
                IgnoreWeight = IgnoreWeightSetting.Value;
                Logger.LogInfo($"Ignore Weight: {IgnoreWeight}");
            };

            IgnoreStamina = IgnoreStaminaSetting.Value;
            IgnoreWeight = IgnoreWeightSetting.Value;

        }

        // Settings
        internal static bool IgnoreStamina { get; set; } = false;
        internal static bool IgnoreWeight { get; set; } = false;
    }
}
