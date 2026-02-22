using HarmonyLib;
using ProjectOrbitalRing.Patches.Logic.PlanetFocus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Reflection.Emit;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.EquatorRing;
using static ProjectOrbitalRing.ProjectOrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine.PostProcessing;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    internal class OrbitalBeacon
    {
        public static Dictionary<FactorySystem, int> SynapticLathePlanet = new Dictionary<FactorySystem, int>();

        [HarmonyPatch(typeof(BeaconComponent), nameof(BeaconComponent.GameTick))]
        [HarmonyPrefix]
        public static bool GameTickPatch(ref BeaconComponent __instance, PlanetFactory factory, PrefabDesc pdesc, EAggressiveLevel agglv, float power, long time)
        {
            if (pdesc.beaconSignalRadius == 0.0f) {
                int num = (int)(time % 60);
                if (num != 0) {
                    return false;
                }
                var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
                if (planetOrbitalRingData == null) return false;
                for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++) {
                    EquatorRing ring = planetOrbitalRingData.Rings[i];
                    if (!ring.IsOneFull()) continue;
                    for (int j = 0; j < ring.Capacity; j++) {
                        var pair = ring.GetPair(j);
                        if (pair.stationType == StationType.GlobalIncCore && pair.OrbitalCorePoolId == __instance.id) {
                            if (power == 1) {
                                if (pdesc.workEnergyPerTick == 4200000) {
                                    ring.incCoreLevel[j] = 1;
                                } else if (pdesc.workEnergyPerTick == 8400000) {
                                    ring.incCoreLevel[j] = 2;
                                } else if (pdesc.workEnergyPerTick == 21000000) {
                                    ring.incCoreLevel[j] = 4;
                                }
                            } else {
                                ring.incCoreLevel[j] = 0;
                            }
                            return false;
                        } else if (pair.stationType == StationType.SynapticLathe && pair.OrbitalCorePoolId == __instance.id) {
                            if (power == 1) {


                                var storage = ring.GetElevatorStorage(j);
                                for (int k = 0; k < storage.Length; k++) {
                                    int itemId = storage[k].itemId;
                                    if (itemId == ProtoID.I湿件主机) {
                                        if (storage[k].count > 33) {
                                            int SynapticLatheSpeed = storage[k].count * 100;
                                            if (!SynapticLathePlanet.ContainsKey(factory.factorySystem)) {
                                                SynapticLathePlanet.Add(factory.factorySystem, SynapticLatheSpeed);
                                            } else {
                                                LogError($"Planet speed {SynapticLathePlanet[factory.factorySystem]}");
                                                SynapticLathePlanet[factory.factorySystem] = SynapticLatheSpeed;
                                            }
                                            storage[k].count -= (int)(storage[k].count * 0.03);
                                        } else if (storage[k].count <= 33 && SynapticLathePlanet.ContainsKey(factory.factorySystem)) {
                                            SynapticLathePlanet.Remove(factory.factorySystem);
                                        }
                                        return false;
                                    }
                                }
                            }
                        } else if (pair.stationType == StationType.BanDFTinderDispatch && pair.OrbitalCorePoolId == __instance.id) {
                            if (power == 1) {
                                if (!BanDFTinderDispatchFromHive.DFTinderShouldNotDispatchStarId.Contains(factory.planet.star.id)) {
                                    BanDFTinderDispatchFromHive.DFTinderShouldNotDispatchStarId.Add(factory.planet.star.id);
                                }
                            }
                        }
                    }
                }
                if (SynapticLathePlanet.ContainsKey(factory.factorySystem)) {
                    SynapticLathePlanet.Remove(factory.factorySystem);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.GameTick))]
        [HarmonyPostfix]
        public static void GameTickPatch(ref DefenseSystem __instance)
        {
            var planetOrbitalRingData = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(__instance.planet.id);
            if (planetOrbitalRingData == null) return;
            int incLevel = 0;
            for (int i = 0; i < planetOrbitalRingData.Rings.Count; i++) {
                EquatorRing ring = planetOrbitalRingData.Rings[i];
                for (int j = 0; j < ring.Capacity; j++) {
                    incLevel = (incLevel < ring.incCoreLevel[j]) ? ring.incCoreLevel[j] : incLevel;
                }
            }
            planetOrbitalRingData.planetIncLevel = incLevel;
        }

        public static void LabResearchInc(ref LabComponent labComponent)
        {
            foreach (var planet in SynapticLathePlanet) {
                if (planet.Key.labPool[labComponent.id].Equals(labComponent)) {
                    labComponent.extraSpeed += planet.Value;
                    //LogError($"planet {planet.Key} speed {planet.Value}");
                }
            }
        }

        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> LabComponent_InternalUpdateResearch_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Cargo), nameof(Cargo.incTableMilli))));

            //matcher.Advance(10).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarga, (byte)0));
            matcher.Advance(10).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0));
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(OrbitalBeacon), nameof(LabResearchInc))));

            //matcher.LogInstructionEnumeration();
            return matcher.InstructionEnumeration();
        }
    }
}
