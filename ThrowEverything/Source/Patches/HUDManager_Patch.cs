using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using ThrowEverything.Models;
using ThrowEverything.Patches;

namespace ThrowEverything.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    public class HUDManager_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Update")]
        static void Update(HUDManager __instance)
        {
            if (Throwable.GetItem() is not GrabbableObject item)
            {
                ControlTips.Clear();
            }
            else
            {
                ControlTips.Set(item);
            }
        }
    }
}
