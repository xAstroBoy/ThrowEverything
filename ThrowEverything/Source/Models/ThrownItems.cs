using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Drawing;
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

            float size = Utils.ItemScale(item) * 2;
            var colliders = item.transform.SphereCastFromCenter(size, 2f, TargetedCollisions);
            foreach (RaycastHit hit in colliders)
            {
                string name = hit.collider.name;

                if (item == hit.collider)
                {
                    Plugin.Logger.LogDebug($"skipping {name} (is itself)");
                    continue;
                }

                if (thrownItem.HasAlreadyHit(hit.collider))
                {
                    Plugin.Logger.LogDebug($"skipping {name} (already been hit)");
                    continue;
                }
                Plugin.Logger.LogInfo($"Hit: {hit.collider.name} with Path {hit.transform.GetPath()}");
                if (hit.transform.TryGetComponent(out IHittable hittable))
                {
                    if (hittable is PlayerControllerB player)
                    {
                        HandlePlayerDamage(thrownItem, player);
                        continue;
                    }
                    if (hittable is EnemyAICollisionDetect EnemyCollider)
                    {
                        HandleEnemyDamage(thrownItem, hittable, EnemyCollider.mainScript);
                        continue;
                    }
                    if (hittable is Landmine landmine)
                    {
                        HandleLandmines(thrownItem, landmine);
                        continue;
                    }
                    if (hittable is Turret Turret)
                    {
                        HandleTurret(thrownItem, Turret);
                        continue;
                    }

                    if (hittable is SpikeRoofTrap Spike)
                    {
                        HandleSpike(thrownItem, Spike);
                        continue;
                    }
                    Plugin.Logger.LogInfo($"Hittable: {hit.collider.name} with Path {hit.transform.GetPath()}");
                    HandleHittableDamage(thrownItem, hittable);
                    continue;
                }

                if (hit.transform.TryGetComponent(out Landmine mine))
                {
                    HandleLandmines(thrownItem, mine);
                    continue;
                }
                if (hit.transform.TryGetComponent(out Turret turret))
                {
                    HandleTurret(thrownItem, turret);
                    continue;
                }
                if (hit.transform.TryGetComponent(out SpikeRoofTrap spike))
                {
                    HandleSpike(thrownItem, spike);
                    continue;
                }

                if (hit.transform.TryGetComponent(out TerminalAccessibleObject door))
                {
                    HandleDoor(thrownItem, door);
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

                Plugin.Logger.LogInfo("dropping to floor");
                item.startFallingPosition = item.transform.localPosition;
                item.FallToGround();
                thrownItem.LandAndRemove();
                break;
            }
        }

        private void HandleLandmines(ThrownItem item, Landmine mine)
        {
            if (item == null) return;
            if (mine == null) return;
            if (mine.HasAlreadyHit(item)) return;

            Plugin.Logger.LogInfo("hitting a landmine");
            if (mine.isLandmineActive())
            {
                Plugin.Logger.LogInfo("BOOM!");
                mine.Explode();
            }
        }

        private void HandleTurret(ThrownItem item, Turret turret)
        {
            if (item == null) return;
            if (turret == null) return;
            if (turret.HasAlreadyHit(item)) return;

            bool is_Key = item.GetItem() is KeyItem;
            if (!is_Key)
            {
                if (turret.isTurretActive())
                {
                    Plugin.Logger.LogInfo("Berserk Mode!");
                    turret.Berserk();
                }
            }
            else
            {
                turret.ToggleTurret(true);
                string state = turret.isTurretActive() ? "On" : "Off";
                Plugin.Logger.LogInfo($"Turning turret {state}");
            }
        }

        private void HandleSpike(ThrownItem item, SpikeRoofTrap spike)
        {
            if (item == null) return;
            if (spike == null) return;
            bool is_Key = item.GetItem() is KeyItem;
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
                spike.ToggleSpikes(true);
                string state = spike.isTrapActive() ? "On" : "Off";
                Plugin.Logger.LogInfo($"Turning Spike {state}");
            }
        }

        private void HandleDoor(ThrownItem item, TerminalAccessibleObject door)
        {
            if (item == null) return;
            if (door == null) return;
            if (!door.isBigDoor) return;
            bool is_Key = item.GetItem() is KeyItem;
            if (is_Key)
            {
                door.ToggleDoor();
                string state = door.isDoorOpen() ? "Open" : "Closed";
                Plugin.Logger.LogInfo($"Setting Door {state}");
            }
        }

        private void HandleEnemyDamage(ThrownItem item, IHittable enemyhitter, EnemyAI enemy)
        {
            if (item == null) return;
            if (enemyhitter == null) return;
            if (enemy == null) return;
            if (enemyhitter.HasAlreadyHit(item)) return;
            int Damage = GetDamage(item, Plugin.IgnoreWeight);
            Plugin.Logger.LogInfo($"damaging an {enemy.enemyType.name} with {Damage} , his Health is ({enemy.enemyHP})");
            if (enemy.isEnemyDead || enemy.enemyHP == 0)
            {
                Plugin.Logger.LogInfo($"Killed {enemy.enemyType.name} with {item.GetItem().itemProperties.itemName}");
                return;
            }
            if (Damage > 2)
            {
                Plugin.Logger.LogInfo($"stunning an enemy {Damage * 5}");
                enemy.SetEnemyStunned(true, Damage * 5);
            }
            enemyhitter.Hit(Damage, item.GetItem().transform.forward, item.GetThrower(), true, -1);
        }

        private void HandlePlayerDamage(ThrownItem item, PlayerControllerB player)
        {
            if (player == null) return;
            if (item == null) return;
            if (player.HasAlreadyHit(item)) return;
            if (player.IsSelf())
            {
                Plugin.Logger.LogDebug($"skipping {item} (is the thrower)");
                return;
            }

            int Damage = GetPlayerDamage(item, Plugin.DamageByWeight);
            Plugin.Logger.LogInfo($"damaging a player {Damage} ({player.health}");
            Utils.DamagePlayer(player, Damage, item.GetItem().transform.forward, item.GetThrower());

            Plugin.Logger.LogInfo($"they now have {player.health} ({player.isPlayerDead})");
            if (player.isPlayerDead || player.health == 0 || player.health - Damage <= 0)
            {
                Plugin.Logger.LogInfo("it killed them (lol)");
            }
        }

        private void HandleHittableDamage(ThrownItem item, IHittable generic)
        {
            if (item == null) return;
            if (generic == null) return;
            if (generic.HasAlreadyHit(item)) return;

            int Damage = GetDamage(item, Plugin.DamageByWeight);
            Plugin.Logger.LogInfo($"damaging a {generic.GetType().Name} with {Damage}");
            generic.Hit(Damage, item.GetItem().transform.forward, item.GetThrower(), true, -1);
        }

        private int GetPlayerDamage(ThrownItem item, bool DamageByWeight)
        {
            if (item == null) return 0;
            float markiplier = item.GetMarkiplier();
            int damage;
            if (!DamageByWeight)
            {
                damage = (int)Math.Round(markiplier * 100);
            }
            else
            {
                damage = 1;
            }
            return damage;
        }

        private int GetDamage(ThrownItem item, bool DamageByWeight)
        {
            if (item == null) return 0;
            float markiplier = item.GetMarkiplier();
            int damage;
            if (!DamageByWeight)
            {
                damage = (int)Math.Round(markiplier * 10);
            }
            else
            {
                damage = Utils.DamageFromWeight(item.GetItem());
            }
            return damage;
        }
    }
}