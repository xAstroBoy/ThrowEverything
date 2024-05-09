using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ThrowEverything.Models
{
    internal class ThrownItem
    {
        readonly GrabbableObject item;
        readonly PlayerControllerB thrower; // this was funny as fuck at like 1am
        readonly float chargeDecimal;
        readonly float markiplier; // same here (markiplier = multiplier = power * charge)
        
        readonly DateTime thrownAt;
        readonly HashSet<Collider> ColliderHit = [];
        readonly HashSet<IHittable> hittables = [];
        internal ThrownItem(GrabbableObject item, PlayerControllerB thrower, float chargeDecimal, float markiplier)
        {
            this.item = item;
            this.thrower = thrower;
            this.chargeDecimal = chargeDecimal;
            this.markiplier = markiplier;

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
            if (item is RagdollGrabbableObject ragdoll)
            {
                Rigidbody rb = ragdoll.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Plugin.Logger.LogInfo($"Removing Thrown Body Kinematic property");
                    rb.isKinematic = false;
                }

            }
            hittables.Clear();
            ColliderHit.Clear();
        }
    }
}
