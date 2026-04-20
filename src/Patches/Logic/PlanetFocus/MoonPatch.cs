using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.AssemblerModule;
using ProjectOrbitalRing.Patches.UI;
using ProjectOrbitalRing.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static ProjectOrbitalRing.ProjectOrbitalRing;

namespace ProjectOrbitalRing.Patches.Logic.PlanetFocus
{
    internal class MoonPatch
    {
        public static ConcurrentDictionary<ValueTuple<int, int>, int> ColliderAccumulatorIncData = new ConcurrentDictionary<ValueTuple<int, int>, int>();

        public static void ApplyPatch(Harmony harmonyInstance)
        {
            // 1. 定义目标方法的参数类型数组（包含out参数）
            Type[] InsertIntoParameterTypes = new Type[]
            {
            typeof(int),
            typeof(int),
            typeof(int),
            typeof(byte),
            typeof(byte),
            typeof(byte).MakeByRefType() // out byte 正确声明
            };
            Type[] InsertInto2ParameterTypes = new Type[]
            {
            typeof(uint),
            typeof(int),
            typeof(int),
            typeof(byte),
            typeof(byte),
            typeof(byte).MakeByRefType() // out byte 正确声明
            };

            Type[] PickFromParameterTypes = new Type[]
            {
            typeof(int),
            typeof(int),
            typeof(int),
            typeof(int[]),
            typeof(byte).MakeByRefType(),
            typeof(byte).MakeByRefType() // out byte 正确声明
            };
            Type[] PickFrom2ParameterTypes = new Type[]
            {
            typeof(uint),
            typeof(int),
            typeof(int),
            typeof(int[]),
            typeof(byte).MakeByRefType(),
            typeof(byte).MakeByRefType() // out byte 正确声明
            };

            // 2. 正确调用GetMethod重载（重点修正这里！）
            // 重载格式：GetMethod(方法名, 绑定标志, 绑定器(传null), 参数类型数组, 参数修饰符(传null))
            MethodInfo pickFromTargetMethod = typeof(PlanetFactory).GetMethod(
                nameof(PlanetFactory.PickFrom),                     // 第一个参数：方法名
                BindingFlags.Public | BindingFlags.Instance,         // 第二个参数：BindingFlags（实例+公共方法）
                null,                                                 // 第三个参数：Binder（通常为null）
                PickFromParameterTypes,                                       // 第四个参数：参数类型数组
                null                                                  // 第五个参数：ParameterModifier[]（通常为null）
            );
            MethodInfo pickFrom2TargetMethod = typeof(PlanetFactory).GetMethod(
                nameof(PlanetFactory.PickFrom),                     // 第一个参数：方法名
                BindingFlags.Public | BindingFlags.Instance,         // 第二个参数：BindingFlags（实例+公共方法）
                null,                                                 // 第三个参数：Binder（通常为null）
                PickFrom2ParameterTypes,                                       // 第四个参数：参数类型数组
                null                                                  // 第五个参数：ParameterModifier[]（通常为null）
            );

            MethodInfo insertIntoTargetMethod = typeof(PlanetFactory).GetMethod(
                nameof(PlanetFactory.InsertInto),                     // 第一个参数：方法名
                BindingFlags.Public | BindingFlags.Instance,         // 第二个参数：BindingFlags（实例+公共方法）
                null,                                                 // 第三个参数：Binder（通常为null）
                InsertIntoParameterTypes,                                       // 第四个参数：参数类型数组
                null                                                  // 第五个参数：ParameterModifier[]（通常为null）
            );
            MethodInfo insertInto2TargetMethod = typeof(PlanetFactory).GetMethod(
                nameof(PlanetFactory.InsertInto),                     // 第一个参数：方法名
                BindingFlags.Public | BindingFlags.Instance,         // 第二个参数：BindingFlags（实例+公共方法）
                null,                                                 // 第三个参数：Binder（通常为null）
                InsertInto2ParameterTypes,                                       // 第四个参数：参数类型数组
                null                                                  // 第五个参数：ParameterModifier[]（通常为null）
            );

            // 3. 容错检查：确认方法是否找到
            if (pickFromTargetMethod == null || pickFrom2TargetMethod == null || insertIntoTargetMethod == null || insertInto2TargetMethod == null) {
                //Debug.LogError($"未找到方法：PlanetFactory.InsertInto，检查参数类型是否匹配！");
                return;
            }

            // 4. 获取Transpiler方法
            MethodInfo pickFromTranspilerMethod = typeof(MoonPatch).GetMethod(
                nameof(FactorySystem_PickFrom_Patch),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodInfo pickFrom2TranspilerMethod = typeof(MoonPatch).GetMethod(
                nameof(FactorySystem_PickFrom2_Patch),
                BindingFlags.Public | BindingFlags.Static
            );

            MethodInfo InsertIntoTranspilerMethod = typeof(MoonPatch).GetMethod(
                nameof(FactorySystem_InsertInto_Patch),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodInfo InsertInto2TranspilerMethod = typeof(MoonPatch).GetMethod(
                nameof(FactorySystem_InsertInto2_Patch),
                BindingFlags.Public | BindingFlags.Static
            );

            if (pickFromTranspilerMethod == null || pickFrom2TranspilerMethod == null || InsertIntoTranspilerMethod == null || InsertInto2TranspilerMethod == null) {
                //Debug.LogError($"未找到Transpiler方法：PlanetFactory_InsertInto_Transpiler！");
                return;
            }

            // 5. 手动打补丁
            harmonyInstance.Patch(
                original: pickFromTargetMethod,
                postfix: new HarmonyMethod(pickFromTranspilerMethod)
                //transpiler: new HarmonyMethod(pickFromTranspilerMethod)
            );
            harmonyInstance.Patch(
                original: pickFrom2TargetMethod,
                postfix: new HarmonyMethod(pickFrom2TranspilerMethod)
                //transpiler: 
            );
            harmonyInstance.Patch(
                original: insertIntoTargetMethod,
                postfix: new HarmonyMethod(InsertIntoTranspilerMethod)
                //transpiler: 
            );
            harmonyInstance.Patch(
                original: insertInto2TargetMethod,
                postfix: new HarmonyMethod(InsertInto2TranspilerMethod)
                //transpiler: 
            );

            //Debug.Log("PlanetFactory.InsertInto 补丁加载成功！");
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.PickFrom))]
        public static void FactorySystem_PickFrom_Patch(PlanetFactory __instance, int entityId, int offset, int filter, int[] needs, ref byte stack, ref byte inc, int __result)
        {
            int beltId = __instance.entityPool[entityId].beltId;
            if (beltId <= 0) {
                int assemblerId = __instance.entityPool[entityId].assemblerId;
                if (assemblerId > 0) {
                    AssemblerComponent assembler = __instance.factorySystem.assemblerPool[assemblerId];
                    // 贫瘠荒漠特质，熔炉产物自带增产
                    if (PlanetThemeUtils.GetVanillaThemeId(__instance.planet) == 11) {
                        if ((assembler.recipeType == ERecipeType.Smelt || assembler.recipeType == (ERecipeType)11) && assembler.speed < 40000) {
                            if (__result != 0) {
                                inc = 4;
                            }
                        }
                    }

                    // 奇异物质，修改抓出电池的增产情况
                    if (assembler.recipeId == 104) {
                        if (__result == 2206) {
                            ValueTuple<int, int> key = new ValueTuple<int, int>(__instance.planetId, assemblerId);
                            bool flag = ColliderAccumulatorIncData.ContainsKey(key) && ColliderAccumulatorIncData[key] >= 4;
                            if (flag) {
                                ColliderAccumulatorIncData[key] -= 4;
                                inc = 4;
                                if (ColliderAccumulatorIncData[key] < 0) {
                                    ColliderAccumulatorIncData[key] = 0;
                                }
                            }

                        }
                    }
                }
            }
        }

        public static void FactorySystem_PickFrom2_Patch(PlanetFactory __instance, uint ioTargetTypedId, int offset, int filter, int[] needs, ref byte stack, ref byte inc, int __result)
        {
            int num = (int)(ioTargetTypedId & 16777215U);
            EFactoryIOTargetType efactoryIOTargetType = (EFactoryIOTargetType)(ioTargetTypedId & 4278190080U);
            if (efactoryIOTargetType <= EFactoryIOTargetType.Silo) {
                if (efactoryIOTargetType <= EFactoryIOTargetType.Assembler) {
                    if (efactoryIOTargetType != EFactoryIOTargetType.Belt) {
                        if (efactoryIOTargetType == EFactoryIOTargetType.Assembler) {
                            ref AssemblerComponent ptr = ref __instance.factorySystem.assemblerPool[num];
                            if (ptr.id <= 0) {
                                return;
                            }
                            // 贫瘠荒漠特质，熔炉产物自带增产
                            if (PlanetThemeUtils.GetVanillaThemeId(__instance.planet) == 11) {
                                if ((ptr.recipeType == ERecipeType.Smelt || ptr.recipeType == (ERecipeType)11) && ptr.speed < 40000) {
                                    if (__result != 0) {
                                        inc = 4;
                                    }
                                }
                            }

                            // 奇异物质，修改抓出电池的增产情况
                            if (ptr.recipeId == 104) {
                                if (__result == 2206) {
                                    ValueTuple<int, int> key = new ValueTuple<int, int>(__instance.planetId, num);
                                    bool flag = ColliderAccumulatorIncData.ContainsKey(key) && ColliderAccumulatorIncData[key] >= 4;
                                    if (flag) {
                                        ColliderAccumulatorIncData[key] -= 4;
                                        inc = 4;
                                        if (ColliderAccumulatorIncData[key] < 0) {
                                            ColliderAccumulatorIncData[key] = 0;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }


        public static void Export(BinaryWriter w)
        {
            w.Write(ColliderAccumulatorIncData.Count);
            foreach (KeyValuePair<ValueTuple<int, int>, int> keyValuePair in ColliderAccumulatorIncData) {
                w.Write(keyValuePair.Key.Item1);
                w.Write(keyValuePair.Key.Item2);
                w.Write(keyValuePair.Value);
            }
        }

        // Token: 0x0600015D RID: 349 RVA: 0x0000FF30 File Offset: 0x0000E130
        public static void Import(BinaryReader r)
        {
            ReInitAll();
            try {
                int num = r.ReadInt32();
                for (int i = 0; i < num; i++) {
                    int planetId = r.ReadInt32();
                    int assemblerId = r.ReadInt32();
                    int AccumulatorInc = r.ReadInt32();
                    ColliderAccumulatorIncData.TryAdd(new ValueTuple<int, int>(planetId, assemblerId), AccumulatorInc);
                }
            } catch (EndOfStreamException) {
            }
        }

        // Token: 0x0600015E RID: 350 RVA: 0x0000FFD4 File Offset: 0x0000E1D4
        public static void IntoOtherSave()
        {
            ReInitAll();
        }

        // Token: 0x0600015F RID: 351 RVA: 0x0000FFDC File Offset: 0x0000E1DC
        private static void ReInitAll()
        {
            ColliderAccumulatorIncData = new ConcurrentDictionary<ValueTuple<int, int>, int>();
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto),
        //    new Type[] { typeof(int), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte) })]
        public static void FactorySystem_InsertInto_Patch(PlanetFactory __instance, int entityId, int offset, int itemId, byte itemCount, byte itemInc, ref byte remainInc, int __result)
        {
            int beltId = __instance.entityPool[entityId].beltId;
            if (beltId <= 0) {
                int[] array = __instance.entityNeeds[entityId];
                int assemblerId = __instance.entityPool[entityId].assemblerId;
                if (assemblerId > 0) {
                    if (array == null) {
                        return;
                    }
                    ref AssemblerComponent ptr = ref __instance.factorySystem.assemblerPool[assemblerId];
                    if (ptr.recipeId == 104 && itemId == 2207) {
                        ValueTuple<int, int> key = new ValueTuple<int, int>(__instance.planetId, assemblerId);
                        ColliderAccumulatorIncData.AddOrUpdate(key, itemInc, (k, v) => v + itemInc);
                    }

                }
            }
        }

        public static void FactorySystem_InsertInto2_Patch(PlanetFactory __instance, uint ioTargetTypedId, int offset, int itemId, byte itemCount, byte itemInc, ref byte remainInc, int __result)
        {
            int num = (int)(ioTargetTypedId & 16777215U);
            EFactoryIOTargetType efactoryIOTargetType = (EFactoryIOTargetType)(ioTargetTypedId & 4278190080U);
            if (efactoryIOTargetType <= EFactoryIOTargetType.Silo) {
                if (efactoryIOTargetType <= EFactoryIOTargetType.Assembler) {
                    if (efactoryIOTargetType != EFactoryIOTargetType.Belt) {
                        if (efactoryIOTargetType == EFactoryIOTargetType.Assembler) {
                            ref AssemblerComponent ptr = ref __instance.factorySystem.assemblerPool[num];
                            if (ptr.id <= 0) {
                                return;
                            }
                            if (ptr.recipeId == 104 && itemId == 2207) {
                                ValueTuple<int, int> key = new ValueTuple<int, int>(__instance.planetId, num);
                                ColliderAccumulatorIncData.AddOrUpdate(key, itemInc, (k, v) => v + itemInc);
                            }
                        }
                    }

                }
            }
        }
    }
}
