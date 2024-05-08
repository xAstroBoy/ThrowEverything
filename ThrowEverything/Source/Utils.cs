using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using ThrowEverything.Models;
using UnityEngine;

namespace ThrowEverything
{
    internal static class Utils
    {

        internal static HashSet<LayerMask> LayerCollision = new HashSet<LayerMask>
        {
            Mask.Room,
            Mask.Terrain,
            Mask.ShipInterior,
            Mask.Ship,
            Mask.MiscLevelGeometry,
            Mask.NavigationSurface,
            Mask.Railing,
            Mask.PlaceableShipObjects,
        };

        internal static LayerMask TargetedCollisions => LayerCollision.ToLayerMask();

        internal static string Name(GrabbableObject item)
        {
            if (item == null) return "NULL";
            if (item.itemProperties == null) return $"{item.name} (props was NULL)";
            return item.itemProperties.name;
        }

        internal static float ItemWeight(GrabbableObject item, bool IgnoreWeight = false)
        {
            if (IgnoreWeight) return 0;
            if (item == null) return 0;
            float ow = item.itemProperties.weight;
            float t = item.itemProperties.twoHanded ? 2 : 1;
            float w = Math.Clamp((ow - 1) * t, 0, 1);
            return w;
        }


        internal static float ItemPower(GrabbableObject item, float powerDecimal, bool inverse = false)
        {
            float w = ItemWeight(item, Plugin.IgnoreWeight);
            float v;
            if (inverse) v = (1 - w) * (1 - w);
            else v = w * w;
            v = Math.Clamp(v, 0, 1);

            return v * powerDecimal;
        }

        internal static float ItemScale(GrabbableObject item)
        {
            return Math.Clamp(item.transform.localScale.magnitude, 0.2f, 3);
        }

        internal static void DamagePlayer(PlayerControllerB player, int damage, Vector3 hitDirection, PlayerControllerB damager)
        {
            if (!player.AllowPlayerDeath() || player.inAnimationWithEnemy)
            {
                return;
            }

            player.DamagePlayerFromOtherClientServerRpc(damage, hitDirection, (int)damager.playerClientId);
        }

        internal static Vector3 FindLandingRay(Vector3 location, bool logging = false)
        {
            Ray landingRay = new(location, Vector3.down); // the ray of where the item will land (basically the location pointing down)
            if (Physics.Raycast(landingRay, out RaycastHit hitInfo, 100f, TargetedCollisions))
            {
                // if we collide with the floor then we return the collision spot elevated a bit
                if (logging) Plugin.Logger.LogDebug("we hit the floor");
                return hitInfo.point + Vector3.up * 0.05f;
            }

            // otherwise we return the destination straight down
            if (logging) Plugin.Logger.LogDebug("we did not hit the floor");
            return landingRay.GetPoint(100f);
        }

        internal static Vector3 GetItemThrowDestination(GrabbableObject item, PlayerControllerB thrower, float chargeDecimal)
        {
            Ray throwRay = new(thrower.gameplayCamera.transform.position, thrower.gameplayCamera.transform.forward); // a ray from in front of the player
            RaycastHit hitInfo; // where the ray collides
            float itemDistance = ItemPower(item, chargeDecimal, true) * 20;
            float distance;
            if (Physics.Raycast(throwRay, out hitInfo, itemDistance, TargetedCollisions))
            {
                // if we collide with a surface then we make the destination the collision
                Plugin.Logger.LogDebug("we hit a surface");
                distance = hitInfo.distance;
            }
            else
            {
                // if we don't then we go the full length
                Plugin.Logger.LogDebug("we did not hit a surface");
                distance = itemDistance;

            }

            distance = Math.Max(0, distance - ItemScale(item) / 2); // we reduce the distance by the item's scale to avoid clipping into (or even through) surfaces
            Plugin.Logger.LogDebug($"throwing {Name(item)} ({item.itemProperties.weight}): {distance} units ({itemDistance}, {ItemScale(item)})");

            Vector3 destination = throwRay.GetPoint(distance);
            return FindLandingRay(destination);
        }

        internal static Vector3 GetItemThrowDestination(ThrownItem thrownItem)
        {
            return GetItemThrowDestination(thrownItem.GetItem(), thrownItem.GetThrower(), thrownItem.GetChargeDecimal());
        }

        // borrowed from a private function in lethal company
        internal static bool CanUseItem(PlayerControllerB player)
        {
            if ((!player.IsOwner || !player.isPlayerControlled || (player.IsServer && !player.isHostPlayerObject)) && !player.isTestingPlayer)
            {
                return false;
            }

            if (!player.isHoldingObject || player.currentlyHeldObjectServer == null)
            {
                return false;
            }

            if (player.quickMenuManager.isMenuOpen)
            {
                return false;
            }

            if (player.isPlayerDead)
            {
                return false;
            }

            if (!player.currentlyHeldObjectServer.itemProperties.usableInSpecialAnimations && (player.isGrabbingObjectAnimation || player.inTerminalMenu || player.isTypingChat || (player.inSpecialInteractAnimation && !player.inShockingMinigame)))
            {
                return false;
            }

            return true;
        }
        internal static PlayerControllerB LocalPlayer => GameNetworkManager.Instance.localPlayerController;

        internal static bool IsSelf(this PlayerControllerB? player) => LocalPlayer is PlayerControllerB localPlayer && player?.actualClientId == localPlayer.actualClientId;

        internal static int DamageFromWeight(GrabbableObject item)
        {
            float weight = Utils.ItemWeight(item, false);
            int damage = (int)((weight - Math.Truncate(weight)) * 10 / 2);
            if(damage == 0) damage = 1;
            return damage;
        }

        

        /// <summary>
        /// This gets the GameObject path 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static string GetPath(this GameObject obj)
        {
            return GetPath(obj.transform);
        }
        /// <summary>
        /// This gets the Transform path
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        internal static string GetPath(this Transform current)
        {
            if (current.parent == null)
                return current.name;
            return GetPath(current.parent) + "/" + current.name;
        }
        internal static RaycastHit[] SphereCastFromCenter(this Transform transform, float sphereRadius = 1.0f, float additionalRange = 25.0f, LayerMask mask = default)
        {
            try
            {
                Vector3 targetPosition = transform.position + transform.forward * additionalRange;
                Vector3 throwDirection = (targetPosition - transform.position).normalized;
                Vector3 center = transform.position;
                return Physics.SphereCastAll(
                    center,
                    sphereRadius,
                    throwDirection,
                    additionalRange,
                    mask
                );
            }
            catch (NullReferenceException)
            {
                return new RaycastHit[0]; // return an empty array
            }
        }

        internal static bool HasAlreadyHit(this IHittable instance, ThrownItem item)
        {
            if(item == null) return false;
            if(instance == null) return false;
            return item.HasAlreadyHit(instance);

        }
    }
}
