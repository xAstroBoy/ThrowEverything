using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using UnityEngine;

namespace ThrowEverything.Models
{
    internal class ThrownItems
    {
        internal static HashSet<LayerMask> LayerCollision = new HashSet<LayerMask>
        {
            Mask.Player,
            Mask.Enemies,
            Mask.EnemiesNotRendered,
            Mask.MapHazards,
        };

        internal static LayerMask TargetedCollisions => LayerCollision.ToLayerMask();


        internal Dictionary<int, ThrownItem> thrownItemsDict = new();

        internal void Update(ThrownItem thrownItem)
        {
            GrabbableObject item = thrownItem.GetItem();
            bool is_Key = item is KeyItem;

            if (item.reachedFloorTarget || thrownItem.IsPanicking())
            {
                thrownItem.LandAndRemove();
                return;
            }

            float size = Utils.ItemScale(item)* 2;
            var colliders = item.transform.SphereCastForward(size, size + 25f, TargetedCollisions);
            foreach (RaycastHit hit in colliders)
            {
                string name = hit.collider.name;

                if (item == hit.collider)
                {
                    Plugin.Logger.LogDebug($"skipping {name} (is itself)");
                    continue;
                }

                hit.transform.TryGetComponent(out PlayerControllerB hitPlayer);
                if (hitPlayer.IsSelf())
                {
                    Plugin.Logger.LogDebug($"skipping {name} (is the thrower)");
                    continue;
                }

                if(hit.collider.transform.gameObject.layer == Mask.MapHazards)
                {
                    // Dump it's path in console
                    Plugin.Logger.LogInfo($"Hit a Map Hazard: {hit.collider.name} with Root {hit.transform.GetPath()}");
                }
                
                // don't forget! this already counts as a hit
                if (thrownItem.CheckIfHitOrAdd(hit.collider))
                {
                    Plugin.Logger.LogDebug($"skipping {name} (already been hit)");
                    continue;
                }
                // print the path of the object
                Plugin.Logger.LogInfo($"Hit: {hit.collider.name} with Path {hit.transform.GetPath()}");
                float markiplier = thrownItem.GetMarkiplier();
                if (hitPlayer != null)
                {
                    int damage;
                    if (!Plugin.IgnoreWeight)
                    {
                        damage = (int)Math.Round(markiplier * 100);
                    }
                    else
                    {
                        damage = Math.Clamp(Utils.DamageFromWeight(item), 1, 3);
                        markiplier = 1;
                    }
                    Plugin.Logger.LogInfo($"damaging a player {damage} ({hitPlayer.health}");
                    Utils.DamagePlayer(hitPlayer, damage, item.transform.forward, thrownItem.GetThrower());

                    Plugin.Logger.LogInfo($"they now have {hitPlayer.health} ({hitPlayer.isPlayerDead})");
                    if (hitPlayer.isPlayerDead || hitPlayer.health == 0 || hitPlayer.health - damage <= 0)
                    {
                        Plugin.Logger.LogInfo("it killed them (lol)");
                        continue; // don't drop to ground if the item killed
                    }
                }
                if (hit.transform.TryGetComponent(out Landmine mine))
                {
                    Plugin.Logger.LogInfo("hitting a landmine");
                    if(mine.isLandmineActive())
                    {
                        Plugin.Logger.LogInfo("BOOM!");
                        mine.TriggerMine();
                    }
                    continue;
                }
                if (hit.transform.TryGetComponent(out Turret turret))
                {
                    if (!is_Key)
                    {
                        if (turret.isTurretActive())
                        {
                            Plugin.Logger.LogInfo("Berserk Mode!");
                            turret.BerserkMode();
                        }
                    }
                    else
                    {
                        turret.ToggleTurret();
                        string state = turret.isTurretActive() ? "On" : "Off";
                        Plugin.Logger.LogInfo($"Turning turret {state}");
                    }
                    continue;
                }
                if (hit.transform.TryGetComponent(out SpikeRoofTrap spike))
                {
                    if (!is_Key)
                    {
                        if (spike.isTrapActive())
                        {
                            Plugin.Logger.LogInfo("Spike Slam!");
                            spike.Slam();
                        }
                    }
                    else
                    {
                        spike.ToggleSpikes();
                        string state = spike.isTrapActive() ? "On" : "Off";
                        Plugin.Logger.LogInfo($"Turning Spike {state}");

                    }
                    continue;
                }
                if (hit.transform.TryGetComponent(out DepositItemsDesk depositItemsDesk))
                {
                    Plugin.Logger.LogInfo("hitting a deposit desk");
                    Utils.LocalPlayer.currentlyHeldObjectServer = item;
                    depositItemsDesk.PlaceItemOnCounter(Utils.LocalPlayer);
                    Utils.LocalPlayer.carryWeight -= item.itemProperties.weight;
                    break; // since we are launching the item to the desk, we don't want to drop it to the floor
                }

                if (hit.transform.TryGetComponent(out IHittable hittable))
                {
                    int damage;
                    if (!Plugin.IgnoreWeight)
                    {
                        damage = (int)Math.Round(markiplier * 10);

                    }
                    else
                    {
                        damage = Utils.DamageFromWeight(item);
                    }
                    Plugin.Logger.LogInfo($"hitting {hit.transform.GetPath()} with Damage {damage}");
                    hittable.Hit(damage, item.transform.forward, thrownItem.GetThrower(), true, -1);
                    continue;
                }
                if (hit.transform.root.TryGetComponent(out EnemyAI enemyAI))
                {
                    int damage;
                    if (!Plugin.IgnoreWeight)
                    {
                        damage = (int)Math.Round(markiplier * 10);

                    }
                    else
                    {
                        damage = Utils.DamageFromWeight(item);
                        markiplier = 1;
                    }
                    float stunTime = markiplier * 5;
                    Plugin.Logger.LogInfo("Markiplier: " + markiplier);
                    Plugin.Logger.LogInfo($"stunning an enemy {stunTime} and Damaging with {damage} 's Force");
                    enemyAI.HitEnemyServerRpc(damage, (int)Utils.LocalPlayer.actualClientId, true, -1);
                    enemyAI.SetEnemyStunned(true, stunTime);

                    if (enemyAI.isEnemyDead || enemyAI.enemyHP == 0)
                    {
                        Plugin.Logger.LogInfo("it killed it");
                        continue; // don't drop to ground if the item killed
                    }
                    continue;
                }
                Plugin.Logger.LogInfo("dropping to floor");
                item.startFallingPosition = item.transform.localPosition;
                item.FallToGround();
                thrownItem.LandAndRemove();
                break;
            }
        }
    }
}
