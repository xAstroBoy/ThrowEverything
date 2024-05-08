using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThrowEverything.Patches;
using UnityEngine;

namespace ThrowEverything.Models
{
    internal class ChargingThrow
    {
        const int MIN_CHARGING_TIME = 500;
        const int MAX_CHARGING_TIME = 2000;

        internal bool isCharging = false;
        internal DateTime startChargingTime;

        internal bool hasRunOutOfStamina = false;
        internal DateTime runOutOfStaminaTime;

        internal bool hasFullyCharged = false;

        GameObject preview;

        internal void StartCharging()
        {
            isCharging = true;
            startChargingTime = DateTime.Now;

            hasRunOutOfStamina = false;
            hasFullyCharged = false;
        }

        internal void Stop()
        {
            isCharging = false;
            hasRunOutOfStamina = false;
            hasFullyCharged = false;
            if(preview != null) UnityEngine.Object.Destroy(preview);

        }

        internal void Exhausted()
        {
            if (Plugin.IgnoreStamina) return;
            if (!hasRunOutOfStamina)
            {
                hasRunOutOfStamina = true;
                runOutOfStaminaTime = DateTime.Now;
            }
        }

        private float GetTime()
        {
            if (hasRunOutOfStamina && !Plugin.IgnoreStamina)
            {
                return (float)(runOutOfStaminaTime - startChargingTime).TotalMilliseconds;
            }
            else
            {
                DateTime now = DateTime.Now;
                return (float)(now - startChargingTime).TotalMilliseconds;
            }
        }

        internal float GetChargeDecimal()
        {
            float itemWeight = Utils.ItemWeight(Throwable.GetItem(), Plugin.IgnoreWeight);
            float percentage = Math.Clamp(GetTime() / Math.Clamp((int)Math.Round(itemWeight * MAX_CHARGING_TIME), MIN_CHARGING_TIME, MAX_CHARGING_TIME), 0, 1);

            if (isCharging && percentage == 1)
            {
                hasFullyCharged = true;
            }

            return percentage;
        }

        internal int GetChargedPercentage()
        {
            return (int)Math.Floor(GetChargeDecimal() * 100);
        }

        internal void DrawLandingCircle()
        {
            if (preview == null)
            {
                preview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                preview.layer = 6;
                preview.GetComponent<Renderer>().material = ShipBuildModeManager.Instance.ghostObjectGreen;
                preview.name = "Throw Previewer";
            }

            GrabbableObject item = Utils.LocalPlayer.currentlyHeldObjectServer;
            ChargingThrow chargingThrow = State.GetChargingThrow();
            float Scale = item == null ? (float)0.2 : Utils.ItemScale(item);
            preview.transform.localScale = new Vector3(Scale, Scale, Scale);
            preview.transform.position = Utils.GetItemThrowDestination(item, Utils.LocalPlayer, chargingThrow.GetChargeDecimal());
        }
    }
}
