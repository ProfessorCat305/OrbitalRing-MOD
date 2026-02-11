using GalacticScale;
using HarmonyLib;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.BattleRelated
{
    internal class StopCircleAround
    {
        //[HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_AttackLaser_Ground))]
        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> RunBehavior_Engage_AttackLaser_Ground_Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var matcher = new CodeMatcher(instructions);

        //    matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_R4, 25f));

        //    matcher.SetAndAdvance(OpCodes.Ldc_R4, 65f);

        //    matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_R4, 35f));

        //    matcher.SetAndAdvance(OpCodes.Ldc_R4, 75f);

        //    matcher.MatchForward(true, new CodeMatch(OpCodes.Ldc_R4, 0f));
        //    object num22 = matcher.Advance(-1).Operand;

        //    matcher.InsertAndAdvance(
        //        new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
        //        new CodeInstruction(OpCodes.Stloc_S, num22)
        //        );
        //    matcher.LogInstructionEnumeration();
        //    return matcher.InstructionEnumeration();
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnitComponent), nameof(UnitComponent.RunBehavior_Engage_AttackLaser_Ground))]
        public static bool RunBehavior_Engage_AttackLaser_Ground_PrePatch(ref UnitComponent __instance, PlanetFactory factory, Mecha mecha, PrefabDesc pdesc, ref CraftData craft, ref CombatSettings combatSettings, ref CombatUpgradeData combatUpgradeData)
        {
            if (__instance.GetTargetPosition_Ground(factory, __instance.hatred.max.target, out var target)) {
                float num = 75f;
                float num2 = 85f;
                EnemyData[] enemyPool = factory.enemyPool;
                CraftData[] craftPool = factory.craftPool;
                int fleetId = craftPool[craft.owner].fleetId;
                ref CraftData reference = ref craftPool[craft.owner];
                if (fleetId > 0) {
                    PrefabDesc obj = PlanetFactory.PrefabDescByModelIndex[reference.modelIndex];
                    num = obj.fleetSensorRange;
                    num2 = obj.fleetMaxActiveArea;
                    if (__instance.hatred.max.value > 800) {
                        float num3 = (float)(enemyPool[__instance.hatred.max.objectId].pos - reference.pos).magnitude;
                        num += num3;
                        num2 += num3;
                    }
                }

                craft.rot.ForwardRight(out var fwd, out var right);
                float craftUnitMaxMovementSpeed = pdesc.craftUnitMaxMovementSpeed;
                float craftUnitMaxMovementAcceleration = pdesc.craftUnitMaxMovementAcceleration;
                float craftUnitAttackRange = pdesc.craftUnitAttackRange0;
                VectorLF3 vectorLF = craft.pos - reference.pos;
                float num4 = (float)vectorLF.magnitude;
                bool flag = num4 > num2;
                if (!flag && !__instance.isRetreating) {
                    short port = craft.port;
                    int num5 = port / 12;
                    int num6 = port % 12;
                    float num7 = factory.planet.realRadius + 25f;//225f;
                    float num8 = factory.planet.realRadius + 12f;//212f;
                    ref VectorLF3 pos = ref craft.pos;
                    ref Quaternion rot = ref craft.rot;
                    ref Vector3 vel = ref craft.vel;
                    
                    float num9 = target.x - (float)pos.x;
                    float num10 = target.y - (float)pos.y;
                    float num11 = target.z - (float)pos.z;
                    float num12 = (float)(pos.x * pos.x + pos.y * pos.y + pos.z * pos.z);
                    float num13 = target.x * target.x + target.y * target.y + target.z * target.z;
                    float num14 = Mathf.Sqrt(num12);
                    float num15 = (num13 - num12) / (num14 * 2f);
                    bool flag2 = num15 > craftUnitAttackRange * 0.9f;
                    num15 = craftUnitAttackRange - num14;
                    if (num14 + num15 > num7) {
                        num15 = num7 - num14;
                    } else if (num14 + num15 < num8) {
                        num15 = num8 - num14;
                    }

                    //num15 += (float)(num5 % 3) + (float)(num6 % 3) * 0.7f - 1.7f;
                    float num16 = (float)pos.x / num14;
                    float num17 = (float)pos.y / num14;
                    float num18 = (float)pos.z / num14;
                    float num19 = num9 * num16 + num10 * num17 + num11 * num18;
                    num19 -= num15 / 1.2f;
                    num9 -= num16 * num19;
                    num10 -= num17 * num19;
                    num11 -= num18 * num19;
                    float num20 = Mathf.Sqrt(num9 * num9 + num10 * num10 + num11 * num11);
                    float num21 = num20;
                    if (num21 < 0f) {
                        num21 = 0f;
                    }

                    float targetDistance = 50f;
                    // 第二步：计算距离差值（近退远进）
                    float distanceDiff = num20 - targetDistance;

                    // 第三步：提取纯水平（切向）方向——彻底移除上下分量（关键！）
                    // 1. 计算目标向量在径向（上下）的投影长度
                    float radialProjection = num9 * num16 + num10 * num17 + num11 * num18;
                    // 2. 从目标向量中减去径向分量，只留水平切向分量
                    float tangX = num9 - radialProjection * num16;
                    float tangY = num10 - radialProjection * num17;
                    float tangZ = num11 - radialProjection * num18;
                    // 3. 归一化水平方向（避免长度异常）
                    float tangLen = Mathf.Sqrt(tangX * tangX + tangY * tangY + tangZ * tangZ);
                    if (tangLen < 0.01f) tangLen = 0.01f; // 防止除以0
                    tangX /= tangLen;
                    tangY /= tangLen;
                    tangZ /= tangLen;

                    // 第四步：仅在水平方向上做距离修正（无上下分量，不会飞上天）
                    num9 = tangX * distanceDiff * 0.3f;
                    num10 = tangY * distanceDiff * 0.3f;
                    num11 = tangZ * distanceDiff * 0.3f;

                    float num22 = 0; // (float)(num6 % 2 * 2 - 1) * ((float)(num6 / 2) * 0.5f + 1f) + (float)num5 * 0.05f;
                    float num23 = num17 * num11 - num10 * num18;
                    float num24 = num18 * num9 - num11 * num16;
                    float num25 = num16 * num10 - num9 * num17;
                    float num26 = craftUnitAttackRange;
                    float num27 = num21 / num26;
                    float num28 = ((num27 > 1f) ? (1f / num27) : (2f - num27));
                    float num29 = num28 * num28;
                    float num30 = (num9 * vel.x + num10 * vel.y + num11 * vel.z) / num20;
                    if (num30 < 0f) {
                        num30 = 0f - num30;
                    }

                    num30 += 0.2f;
                    num30 /= craftUnitMaxMovementSpeed;
                    float num31 = num29 * num30 * num26 * num22 / Mathf.Sqrt(num23 * num23 + num24 * num24 + num25 * num25);
                    num23 *= num31;
                    num24 *= num31;
                    num25 *= num31;
                    num9 += num23;
                    num10 += num24;
                    num11 += num25;
                    float num32 = Mathf.Sqrt(num9 * num9 + num10 * num10 + num11 * num11);
                    float num33 = craftUnitMaxMovementSpeed / num32;
                    num9 *= num33;
                    num10 *= num33;
                    num11 *= num33;
                    float num34 = 0;// 360 - (UnitComponent.gameTick + 35L * (long)num6) % 420;
                    if (num34 < 0f) {
                        num34 = 0f - num34;
                    }

                    num34 = 60f - num34;
                    if (num34 < 0f) {
                        num34 = 0f;
                    }

                    num34 /= 5f;
                    float num35 = num34;
                    float num36 = (num20 - 5f) / (num35 * 1.2f + 5f);
                    num36 = ((!(num36 > 0.28f)) ? 0.28f : ((num36 > 1f) ? 1f : num36));
                    num36 *= num36;
                    float num37 = num20 / craftUnitMaxMovementSpeed;
                    float num38 = ((!(num37 > 2f)) ? 2f : ((num37 > 16f) ? 16f : num37));
                    num38 /= 2f;
                    craftUnitMaxMovementAcceleration /= num38;
                    craftUnitMaxMovementAcceleration *= num36;
                    float num39 = num9 - vel.x;
                    float num40 = num10 - vel.y;
                    float num41 = num11 - vel.z;
                    float num42 = Mathf.Sqrt(num39 * num39 + num40 * num40 + num41 * num41) / (craftUnitMaxMovementAcceleration * (1f / 60f));
                    if (num42 > 1f) {
                        num39 /= num42;
                        num40 /= num42;
                        num41 /= num42;
                    }

                    vel.x += num39;
                    vel.y += num40;
                    vel.z += num41;
                    VectorLF3 vectorLF2 = -vectorLF.normalized;
                    float num43 = 0; // num4 / num2;
                    if (num4 > num) {
                        num43 *= 10f;
                    }

                    vel.x += (float)vectorLF2.x * num43 * 0.15f;
                    vel.y += (float)vectorLF2.y * num43 * 0.15f;
                    vel.z += (float)vectorLF2.z * num43 * 0.15f;
                    __instance.speed = Mathf.Sqrt(vel.x * vel.x + vel.y * vel.y + vel.z * vel.z);
                    if (__instance.speed < craftUnitMaxMovementSpeed + 4f) {
                        float num44 = num34 * (1.45f + (float)num6 * 0.05f) * (1f / 60f) / __instance.speed;
                        //float num44 = 0.01f;
                        vel.x += vel.x * num44;
                        vel.y += vel.y * num44;
                        vel.z += vel.z * num44;
                    }

                    pos.x += (double)vel.x * (1.0 / 60.0);
                    pos.y += (double)vel.y * (1.0 / 60.0);
                    pos.z += (double)vel.z * (1.0 / 60.0);
                    //if (__instance.speed > 0.1f) {
                    //    Maths.LookRotation(vel.x / __instance.speed, vel.y / __instance.speed, vel.z / __instance.speed, num16, num17, num18, out rot);
                    //}

                    __instance.anim = 0f;
                    __instance.isShooting0 = !flag2 && num21 < craftUnitAttackRange;
                    if (__instance.isShooting0) {
                        __instance.anim = 1f - (float)__instance.fire0 / (float)pdesc.craftUnitRoundInterval0 * 2f;
                        if (__instance.anim != 0f && __instance.fire0 <= pdesc.craftUnitROF0 * 4) {
                            SkillTargetLocal skillTargetLocal = __instance.hatred.max.skillTargetLocal;
                            Vector3 endPos = target;
                            if (!enemyPool[skillTargetLocal.id].dynamic) {
                                endPos += endPos.normalized * (SkillSystem.RoughHeightByModelIndex[enemyPool[skillTargetLocal.id].modelIndex] * 0.4f);
                            }

                            bool flag3 = true;
                            int craftUnitFireEnergy = pdesc.craftUnitFireEnergy0;
                            if (reference.owner < 0) {
                                lock (mecha) {
                                    if (mecha.coreEnergy < (double)craftUnitFireEnergy) {
                                        flag3 = false;
                                    }
                                }
                            } else if (reference.owner > 0) {
                                ref EntityData reference2 = ref factory.entityPool[reference.owner];
                                if (factory.defenseSystem.battleBases.buffer[reference2.battleBaseId].energy < craftUnitFireEnergy) {
                                    flag3 = false;
                                }
                            }
                            if (__instance.fire0 <= 0 && flag3) {
                                ref LocalLaserOneShot reference3 = ref factory.skillSystem.fighterLasers.Add();
                                reference3.astroId = factory.planetId;
                                reference3.hitIndex = 16;
                                reference3.beginPos = (Vector3)craft.pos + craft.vel * 0.03333f;
                                reference3.target = skillTargetLocal;
                                reference3.endPos = endPos;
                                reference3.damage = (int)((float)pdesc.craftUnitAttackDamage0 * combatUpgradeData.combatDroneDamageRatio * combatUpgradeData.energyDamageScale + 0.5f);
                                reference3.mask = ETargetTypeMask.Enemy;
                                reference3.caster.type = ETargetType.Craft;
                                reference3.caster.id = __instance.craftId;
                                reference3.life = 15;
                                __instance.fire0 += pdesc.craftUnitRoundInterval0;
                                __instance.hatred.HateTarget(skillTargetLocal.type, skillTargetLocal.id, 40, 300, EHatredOperation.Add);
                                if (reference.owner < 0) {
                                    lock (mecha) {
                                        mecha.coreEnergy -= craftUnitFireEnergy;
                                        mecha.MarkEnergyChange(12, -craftUnitFireEnergy);
                                    }
                                } else if (reference.owner > 0) {
                                    ref EntityData reference4 = ref factory.entityPool[reference.owner];
                                    factory.defenseSystem.battleBases.buffer[reference4.battleBaseId].energy -= craftUnitFireEnergy;
                                }

                                ref EnemyData reference5 = ref enemyPool[skillTargetLocal.id];
                                DFGBaseComponent dFGBaseComponent = factory.enemySystem.bases[reference5.owner];
                                if (dFGBaseComponent != null && dFGBaseComponent.id == reference5.owner) {
                                    factory.skillSystem.AddGroundEnemyHatred(dFGBaseComponent, ref reference5, ETargetType.Craft, __instance.craftId);
                                }
                            }
                        }
                    }
                } else {
                    if (flag && !__instance.isRetreating) {
                        __instance.hatred.ClearMax();
                    }

                    __instance.isRetreating = true;
                    VectorLF3 pos2 = craftPool[craft.owner].pos;
                    VectorLF3 normalized = (craft.pos - pos2).normalized;
                    VectorLF3 vectorLF3 = normalized * num + pos2;
                    Quaternion quaternion = Quaternion.LookRotation(-normalized, craft.pos);
                    Vector3 vector = default(Vector3);
                    float craftUnitMaxMovementSpeed2 = pdesc.craftUnitMaxMovementSpeed;
                    float craftUnitMaxMovementAcceleration2 = pdesc.craftUnitMaxMovementAcceleration;
                    ref FleetComponent reference6 = ref factory.combatGroundSystem.fleets.buffer[fleetId];
                    ref VectorLF3 pos3 = ref craft.pos;
                    ref Quaternion rot2 = ref craft.rot;
                    ref Vector3 vel2 = ref craft.vel;
                    Vector3 vector2 = new Vector3((float)(vectorLF3.x - pos3.x), (float)(vectorLF3.y - pos3.y), (float)(vectorLF3.z - pos3.z));
                    float num45 = Mathf.Sqrt(vector2.x * vector2.x + vector2.y * vector2.y + vector2.z * vector2.z);
                    float magnitude = vel2.magnitude;
                    float num46 = ((magnitude > 2f) ? magnitude : 2f);
                    float num47 = num45 / num46;
                    num47 -= 1f / 60f;
                    if (num47 < 0f) {
                        num47 = 0f;
                    }

                    float num48 = ((num47 > 1f) ? 1f : num47) * 0.3f;
                    Vector3 vector3 = new Vector3((float)vectorLF3.x - vector.x * num48, (float)vectorLF3.y - vector.y * num48, (float)vectorLF3.z - vector.z * num48);
                    Vector3 vector4 = new Vector3(vector3.x - (float)pos3.x, vector3.y - (float)pos3.y, vector3.z - (float)pos3.z);
                    float num49 = Mathf.Sqrt(vector4.x * vector4.x + vector4.y * vector4.y + vector4.z * vector4.z);
                    if (num49 > 0f) {
                        vector4.x /= num49;
                        vector4.y /= num49;
                        vector4.z /= num49;
                    }

                    float num50 = ((!(num47 > 2f)) ? 2f : ((num47 > 6f) ? 6f : num47));
                    num50 /= 2f;
                    craftUnitMaxMovementAcceleration2 /= num50;
                    float num51 = 0f;
                    float num52 = num47 / 0.5f - 0.02f;
                    if (num52 <= 0f) {
                        vel2.x = vector.x;
                        vel2.y = vector.y;
                        vel2.z = vector.z;
                    } else {
                        Vector3 vector5 = new Vector3(vector4.x * craftUnitMaxMovementSpeed2, vector4.y * craftUnitMaxMovementSpeed2, vector4.z * craftUnitMaxMovementSpeed2);
                        if (num52 < 1f) {
                            float num53 = 1f - num52;
                            vector5.x = vector5.x * num52 + vector.x * num53;
                            vector5.y = vector5.y * num52 + vector.y * num53;
                            vector5.z = vector5.z * num52 + vector.z * num53;
                        }

                        Vector3 vector6 = new Vector3(vector5.x - vel2.x, vector5.y - vel2.y, vector5.z - vel2.z);
                        num51 = Mathf.Sqrt(vector6.x * vector6.x + vector6.y * vector6.y + vector6.z * vector6.z);
                        float num54 = num51 / (craftUnitMaxMovementAcceleration2 * (1f / 60f));
                        if (num54 > 1f) {
                            vector6.x /= num54;
                            vector6.y /= num54;
                            vector6.z /= num54;
                        }

                        vel2.x += vector6.x;
                        vel2.y += vector6.y;
                        vel2.z += vector6.z;
                        float num55 = (num49 - 2f) / 15f;
                        if (num55 > 0f) {
                            float num56 = (float)(pos3.x * pos3.x + pos3.y * pos3.y + pos3.z * pos3.z);
                            float num57 = vector3.x * vector3.x + vector3.y * vector3.y + vector3.z * vector3.z - num56;
                            float num58 = Mathf.Sqrt(num56);
                            Vector3 vector7 = new Vector3((float)pos3.x / num58, (float)pos3.y / num58, (float)pos3.z / num58);
                            float num59 = vel2.x * vector7.x + vel2.y * vector7.y + vel2.z * vector7.z;
                            if (num59 * num57 < 0f) {
                                if (num55 >= 1f) {
                                    vel2.x -= vector7.x * num59;
                                    vel2.y -= vector7.y * num59;
                                    vel2.z -= vector7.z * num59;
                                } else {
                                    num55 *= num55;
                                    num59 *= num55;
                                    vel2.x -= vector7.x * num59;
                                    vel2.y -= vector7.y * num59;
                                    vel2.z -= vector7.z * num59;
                                }
                            }
                        }
                    }

                    pos3.x += (double)vel2.x * (1.0 / 60.0);
                    pos3.y += (double)vel2.y * (1.0 / 60.0);
                    pos3.z += (double)vel2.z * (1.0 / 60.0);
                    bool flag4 = false;
                    bool flag5 = false;
                    float num60 = num47 + num51 / craftUnitMaxMovementAcceleration2;
                    float num61 = num60 / 0.15f - 0.04f;
                    if (num61 < 1f) {
                        if (num61 <= 0f) {
                            pos3.x = vectorLF3.x;
                            pos3.y = vectorLF3.y;
                            pos3.z = vectorLF3.z;
                            if (reference6.owner > 0) {
                                __instance.behavior = EUnitBehavior.KeepForm;
                            }

                            flag4 = true;
                        } else {
                            float num62 = 1f - num61;
                            pos3.x = pos3.x * (double)num61 + vectorLF3.x * (double)num62;
                            pos3.y = pos3.y * (double)num61 + vectorLF3.y * (double)num62;
                            pos3.z = pos3.z * (double)num61 + vectorLF3.z * (double)num62;
                        }
                    }

                    float num63 = num60 / 0.65f - 0.04f;
                    if (num63 <= 0f) {
                        rot2 = quaternion;
                        flag5 = true;
                    } else {
                        if (vel2.x * vel2.x + vel2.y * vel2.y + vel2.z * vel2.z > 0.01f) {
                            Quaternion b = Quaternion.LookRotation(vel2, pos3);
                            rot2 = Quaternion.Slerp(rot2, b, 0.1f);
                        }

                        if (num63 < 1f) {
                            rot2 = Quaternion.Slerp(quaternion, rot2, num63 * num63);
                        }
                    }

                    if (flag4 && flag5) {
                        __instance.isRetreating = false;
                    }

                    __instance.anim = 0f;
                    __instance.steering = 0f;
                    __instance.speed = magnitude;
                    Vector3 vector8 = craft.pos;
                    float x = vector8.x;
                    float y = vector8.y;
                    float z = vector8.z;
                    float num64 = craftUnitAttackRange * craftUnitAttackRange;
                    ref HatredTarget reference7 = ref __instance.hatred.max;
                    __instance.isShooting0 = false;
                    for (int i = 0; i < 8; i++) {
                        switch (i) {
                            case 0:
                                reference7 = ref __instance.hatred.max;
                                break;
                            case 1:
                                reference7 = ref __instance.hatred.h1;
                                break;
                            case 2:
                                reference7 = ref __instance.hatred.h2;
                                break;
                            case 3:
                                reference7 = ref __instance.hatred.h3;
                                break;
                            case 4:
                                reference7 = ref __instance.hatred.h4;
                                break;
                            case 5:
                                reference7 = ref __instance.hatred.h5;
                                break;
                            case 6:
                                reference7 = ref __instance.hatred.h6;
                                break;
                            case 7:
                                reference7 = ref __instance.hatred.min;
                                break;
                        }

                        if (reference7.targetType != ETargetType.Enemy) {
                            continue;
                        }

                        int objectId = reference7.objectId;
                        ref EnemyData reference8 = ref enemyPool[objectId];
                        if (reference8.id == objectId && !reference8.isInvincible) {
                            float num65 = (float)reference8.pos.x;
                            float num66 = (float)reference8.pos.y;
                            float num67 = (float)reference8.pos.z;
                            float num68 = num65 - x;
                            float num69 = num66 - y;
                            float num70 = num67 - z;
                            if (!(num68 * num68 + num69 * num69 + num70 * num70 > num64)) {
                                __instance.isShooting0 = true;
                                break;
                            }
                        }
                    }

                    if (__instance.isShooting0) {
                        __instance.anim = 1f - (float)__instance.fire0 / (float)pdesc.craftUnitRoundInterval0 * 2f;
                        if (__instance.anim != 0f && __instance.fire0 <= pdesc.craftUnitROF0 * 4) {
                            SkillTargetLocal skillTargetLocal2 = reference7.skillTargetLocal;
                            vectorLF3 = enemyPool[skillTargetLocal2.id].pos;
                            Vector3 endPos2 = vectorLF3;
                            if (!enemyPool[skillTargetLocal2.id].dynamic) {
                                endPos2 += endPos2.normalized * (SkillSystem.RoughHeightByModelIndex[enemyPool[skillTargetLocal2.id].modelIndex] * 0.4f);
                            }

                            bool flag6 = true;
                            int craftUnitFireEnergy2 = pdesc.craftUnitFireEnergy0;
                            if (reference.owner < 0) {
                                lock (mecha) {
                                    if (mecha.coreEnergy < (double)craftUnitFireEnergy2) {
                                        flag6 = false;
                                    }
                                }
                            } else if (reference.owner > 0) {
                                ref EntityData reference9 = ref factory.entityPool[reference.owner];
                                if (factory.defenseSystem.battleBases.buffer[reference9.battleBaseId].energy < craftUnitFireEnergy2) {
                                    flag6 = false;
                                }
                            }

                            if (__instance.fire0 <= 0 && flag6) {
                                ref LocalLaserOneShot reference10 = ref factory.skillSystem.fighterLasers.Add();
                                reference10.astroId = factory.planetId;
                                reference10.hitIndex = 16;
                                reference10.beginPos = (Vector3)craft.pos + craft.vel * 0.03333f;
                                reference10.target = skillTargetLocal2;
                                reference10.endPos = endPos2;
                                reference10.damage = (int)((float)pdesc.craftUnitAttackDamage0 * combatUpgradeData.combatDroneDamageRatio * combatUpgradeData.energyDamageScale + 0.5f);
                                reference10.mask = ETargetTypeMask.Enemy;
                                reference10.caster.type = ETargetType.Craft;
                                reference10.caster.id = __instance.craftId;
                                reference10.life = 15;
                                __instance.fire0 += pdesc.craftUnitRoundInterval0;
                                if (reference.owner < 0) {
                                    lock (mecha) {
                                        mecha.coreEnergy -= craftUnitFireEnergy2;
                                        mecha.MarkEnergyChange(12, -craftUnitFireEnergy2);
                                    }
                                } else if (reference.owner > 0) {
                                    ref EntityData reference11 = ref factory.entityPool[reference.owner];
                                    factory.defenseSystem.battleBases.buffer[reference11.battleBaseId].energy -= craftUnitFireEnergy2;
                                }

                                ref EnemyData reference12 = ref enemyPool[skillTargetLocal2.id];
                                DFGBaseComponent dFGBaseComponent2 = factory.enemySystem.bases[reference12.owner];
                                if (dFGBaseComponent2 != null && dFGBaseComponent2.id == reference12.owner) {
                                    factory.skillSystem.AddGroundEnemyHatred(dFGBaseComponent2, ref reference12, ETargetType.Craft, __instance.craftId);
                                }
                            }
                        }
                    }
                }

                __instance.steering = Vector3.Dot(craft.rot.Forward() - fwd, right);
            } else {
                __instance.RunBehavior_Engage_EmptyHatred(ref craft);
            }
            return false;
        }
    }
}
