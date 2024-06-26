﻿using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ThrowEverything.Models
{
    internal class ThrownItem
    {
        internal readonly GrabbableObject item;
        internal readonly PlayerControllerB thrower; // this was funny as fuck at like 1am
        internal readonly float chargeDecimal;
        internal readonly float markiplier; // same here (markiplier = multiplier = power * charge)
        internal readonly Rigidbody rigidbody;
        internal readonly DateTime thrownAt;
        internal readonly HashSet<Collider> ColliderHit = [];
        internal readonly HashSet<IHittable> hittables = [];
        internal ThrownItem(GrabbableObject item, PlayerControllerB thrower, float chargeDecimal, float markiplier)
        {
            this.item = item;
            this.thrower = thrower;
            this.chargeDecimal = chargeDecimal;
            this.markiplier = markiplier;
            this.rigidbody = item.transform.GetGetInChildrens_OrParent<Rigidbody>();
            thrownAt = DateTime.Now;
        }

        internal GrabbableObject GetItem()
        {
            return item;
        }

        internal PlayerControllerB GetThrower()
        {
            return thrower;
        }

        internal float GetMarkiplier()
        {
            return markiplier;
        }

        internal float GetChargeDecimal()
        {
            return chargeDecimal;
        }

        internal bool HasAlreadyHit(Collider hittable)
        {
            bool hasHit = ColliderHit.Contains(hittable);
            if (hasHit)
            {
                return true;
            }
            else
            {
                ColliderHit.Add(hittable);
                return false;
            }
        }
        internal bool HasAlreadyHit(IHittable hittable)
        {
            bool hasHit = hittables.Contains(hittable);
            if (hasHit)
            {
                return true;
            }
            else
            {
                hittables.Add(hittable);
                return false;
            }
        }

        internal bool IsPanicking()
        {
            return (DateTime.Now - thrownAt).TotalMilliseconds >= 5000;
        }

        internal void LandAndRemove()
        {
            float loudness = 1 - (1 - markiplier) * (1 - markiplier);
            Plugin.Logger.LogInfo($"playing sound for {item.name} at markiplier {loudness}");
            RoundManager.Instance.PlayAudibleNoise(item.transform.position, Math.Clamp(loudness * 50, 8f, 50f), Math.Clamp(loudness, 0.5f, 1f), 0, item.isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
            State.GetThrownItems().thrownItemsDict.Remove(item.GetInstanceID());
            hittables.Clear();
            ColliderHit.Clear();
        }
    }
}
