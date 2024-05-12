using GameNetcodeStuff;
using HarmonyLib;
using ThrowEverything.Models;
using UnityEngine;

namespace ThrowEverything.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerB_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        private static void Update(PlayerControllerB __instance)
        {
            if (!__instance.IsSelf()) return;

            ChargingThrow chargingThrow = State.GetChargingThrow();
            if (Plugin.IgnoreStamina)
            {
                if (chargingThrow.isCharging)
                {
                    chargingThrow.DrawLandingCircle();
                }
                return;
            }
            if (chargingThrow.isCharging)
            {
                chargingThrow.DrawLandingCircle();

                if (!chargingThrow.hasFullyCharged)
                {
                    __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter - (0.01f / 4), 0f, 1f);
                }
                else
                {
                    __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter - (0.01f / 8), 0f, 1f);
                }
            }

            if (__instance.sprintMeter < 0.3f || __instance.isExhausted)
            {
                chargingThrow.Exhausted();
            }
            else
            {
                chargingThrow.Reset();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
        private static void ScrollMouse_performed(PlayerControllerB __instance)
        {
            if (!__instance.IsSelf()) return;
            if (State.GetChargingThrow().isCharging)
            {
                __instance.isGrabbingObjectAnimation = false; // put it back (hopefully to the way it was)
            }
            if (__instance.currentlyHeldObjectServer == null)
            {
                ControlTips.Clear();
            }
        }
    }
}