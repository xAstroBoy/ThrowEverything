using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace ThrowEverything.Models
{
    internal class Throwable
    {



        internal static GrabbableObject GetItem()
        {
            return Utils.LocalPlayer.currentlyHeldObjectServer;
        }

        internal static void StartThrowing(CallbackContext ctx)
        {
            if (!Utils.CanUseItem(Utils.LocalPlayer))
            {
                Plugin.Logger.LogInfo("cannot use item");
                return;
            }

            Utils.LocalPlayer.isGrabbingObjectAnimation = true;

            State.GetChargingThrow().StartCharging();
        }

        internal static GrabbableObject HeldItem()
        {
            return Utils.LocalPlayer.currentlyHeldObjectServer;
        }

        internal static void Throw(CallbackContext ctx)
        {
            Utils.LocalPlayer.isGrabbingObjectAnimation = false;

            if (Utils.LocalPlayer == null || HeldItem() == null)
            {
                State.ClearHeldThrowable();
                return;
            }

            ChargingThrow chargingThrow = State.GetChargingThrow();
            if (!chargingThrow.isCharging)
            {
                // prevents players from switching items while holding down the charge button
                Plugin.Logger.LogInfo($"tried to throw without charging");
                return;
            }

            float chargeDecimal = chargingThrow.GetChargeDecimal();
            float markiplier = Utils.ItemPower(HeldItem(), chargeDecimal, false);
            ThrownItem thrownItem = new(HeldItem(), HeldItem().playerHeldBy, chargeDecimal, markiplier);
            State.GetThrownItems().thrownItemsDict.Add(HeldItem().GetInstanceID(), thrownItem);
            Rigidbody heldRigidbody = thrownItem.rigidbody;
            if (heldRigidbody != null)
            {
                Vector3 throwDirection = Utils.GetItemThrowDestination(thrownItem) - heldRigidbody.position;
                Vector3 throwForce = throwDirection.normalized * markiplier;
                heldRigidbody.AddForce(throwForce, ForceMode.Impulse);
            }
            HeldItem().playerHeldBy.DiscardHeldObject(placeObject: true, null, Utils.GetItemThrowDestination(thrownItem));
        }

        internal static void HookEvents()
        {
            InputSettings.Instance.ThrowItem.started += StartThrowing;
            InputSettings.Instance.ThrowItem.canceled += Throw;
        }
    }
}
