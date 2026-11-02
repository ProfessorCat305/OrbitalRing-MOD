using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;

namespace ProjectOrbitalRing.Patches.Logic.OrbitalRing
{
    [HarmonyPatch]
    internal static class StructuralTrackConstruction
    {
        private sealed class FactoryState
        {
            public FactoryState()
            {
            }

            internal bool Initialized;
            internal bool Initializing;
            internal readonly HashSet<int> FailedNodes = new HashSet<int>();
        }

        private struct Node
        {
            internal int ObjectId;
            internal int BeltId;
            internal int Previous;
            internal int Next;
            internal bool Assignable;
            internal bool Canonical;
            internal int ExternalPorts;
        }

        private static readonly ConditionalWeakTable<PlanetFactory, FactoryState> factories =
            new ConditionalWeakTable<PlanetFactory, FactoryState>();
        private static readonly List<(BuildPreview Preview, BuildPreview Other, int OtherObject)> previewLinks =
            new List<(BuildPreview, BuildPreview, int)>();
        private static readonly Dictionary<(BuildPreview Preview, bool Forward),
            (BuildPreview Target, int ObjectId, bool Declared)> previewPorts =
            new Dictionary<(BuildPreview, bool), (BuildPreview, int, bool)>();
        private static int normalizationDepth;

        internal static void Update()
        {
            if (StructuralTrackPolicy.Enabled && GameMain.isRunning)
            {
                EnsureFactory(GameMain.localPlanet?.factory);
            }
        }

        private static void EnsureFactory(PlanetFactory factory)
        {
            if (factory?.cargoTraffic != null && StructuralTrackPolicy.Enabled &&
                !factories.GetOrCreateValue(factory).Initialized)
            {
                NormalizeFactory(factory);
            }
        }

        internal static bool NormalizeFactory(PlanetFactory factory)
        {
            if (!StructuralTrackPolicy.Enabled || factory?.cargoTraffic?.beltPool == null)
            {
                return false;
            }
            FactoryState state = factories.GetOrCreateValue(factory);
            if (state.Initializing)
            {
                return false;
            }
            state.Initializing = true;
            bool success = true;
            try
            {
                var visited = new HashSet<int>();
                for (int entityId = 1; entityId < factory.entityCursor; entityId++)
                {
                    NormalizeUnvisited(entityId);
                }
                for (int prebuildId = 1; prebuildId < factory.prebuildCursor; prebuildId++)
                {
                    NormalizeUnvisited(-prebuildId);
                }
                state.Initialized = true;
                return success;

                void NormalizeUnvisited(int objectId)
                {
                    if (!visited.Contains(objectId) && StructuralTrackPolicy.IsTrackObject(factory, objectId))
                    {
                        success &= NormalizeNodes(factory, CollectNodes(factory, objectId, visited));
                    }
                }
            }
            finally
            {
                state.Initializing = false;
            }
        }

        internal static bool NormalizeComponent(PlanetFactory factory, int entityId)
        {
            return StructuralTrackPolicy.Enabled && StructuralTrackPolicy.IsTrackObject(factory, entityId) &&
                !factories.GetOrCreateValue(factory).FailedNodes.Contains(entityId) &&
                NormalizeNodes(factory, CollectNodes(factory, entityId, new HashSet<int>()));
        }

        private static List<Node> CollectNodes(PlanetFactory factory, int first, HashSet<int> visited)
        {
            var nodes = new List<Node> { new Node { ObjectId = first } };
            visited.Add(first);
            for (int index = 0; index < nodes.Count; index++)
            {
                Node node = ReadNode(factory, nodes[index].ObjectId, nodes, visited);
                nodes[index] = node;
            }
            return nodes;
        }

        private static Node ReadNode(PlanetFactory factory, int objectId, List<Node> component = null,
            HashSet<int> visited = null)
        {
            int beltId = BeltId(factory, objectId);
            var node = new Node
            {
                ObjectId = objectId,
                BeltId = StructuralTrackPolicy.IsTrackBelt(factory.cargoTraffic, beltId) ? beltId : 0,
                Assignable = true,
                Canonical = true,
            };
            for (int slot = 0; slot < 16; slot++)
            {
                factory.ReadObjectConn(objectId, slot, out bool output, out int other, out int otherSlot);
                if (other == 0)
                {
                    continue;
                }
                if (slot >= 4 || other == objectId || !StructuralTrackPolicy.CanConnect(factory, objectId, other,
                    out Vector3 position, out Vector3 otherPosition, out _))
                {
                    node.ExternalPorts |= 1 << slot;
                    node.Canonical = false;
                    continue;
                }
                if (visited != null && visited.Add(other))
                {
                    component.Add(new Node { ObjectId = other });
                }
                if (!TryGetDirection(position, otherPosition, out bool forward))
                {
                    node.Assignable = node.Canonical = false;
                    continue;
                }

                ref int neighbor = ref (forward ? ref node.Next : ref node.Previous);
                if (neighbor != 0 && neighbor != other)
                {
                    node.Assignable = false;
                }
                else
                {
                    neighbor = other;
                }

                int canonicalSlot = forward ? 0 : 1;
                node.Canonical &= output == forward && slot == canonicalSlot && otherSlot == 1 - canonicalSlot;
            }
            node.Canonical = node.Canonical && (objectId < 0 || NativePortsMatch(factory, node));
            return node;
        }

        private static bool NativePortsMatch(PlanetFactory factory, Node node)
        {
            if (node.BeltId == 0)
            {
                return false;
            }

            BeltComponent belt = factory.cargoTraffic.beltPool[node.BeltId];
            int previous = BeltId(factory, node.Previous);
            int inputs = (belt.rightInputId == 0 ? 0 : 1) + (belt.backInputId == 0 ? 0 : 1) +
                (belt.leftInputId == 0 ? 0 : 1);
            // Native path geometry can assign the sole input to any of its three slots.
            return belt.outputId == BeltId(factory, node.Next) && belt.mainInputId == previous &&
                inputs == (previous == 0 ? 0 : 1);
        }

        private static bool IsCanonical(PlanetFactory factory, int objectId) => ReadNode(factory, objectId).Canonical;

        private static int BeltId(PlanetFactory factory, int objectId) =>
            objectId > 0 ? factory.entityPool[objectId].beltId : 0;

        private static bool NormalizeNodes(PlanetFactory factory, List<Node> nodes)
        {
            bool normalized;
            try
            {
                normalized = NormalizeNodesCore(factory, nodes);
            }
            catch (Exception error)
            {
                global::ProjectOrbitalRing.ProjectOrbitalRing.logger?.LogError(
                    "Orbital track normalization failed: planet=" + factory.planetId +
                    ", node=" + nodes[0].ObjectId + "\n" + error);
                normalized = false;
            }
            if (!normalized)
            {
                FactoryState state = factories.GetOrCreateValue(factory);
                foreach (Node node in nodes)
                {
                    state.FailedNodes.Add(node.ObjectId);
                }
            }
            return normalized;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConn))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConnDirect))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.ClearObjectConn), typeof(int), typeof(int))]
        private static void ConnectionsChanged(PlanetFactory __instance, int objId)
        {
            if (factories.TryGetValue(__instance, out FactoryState state) && state.FailedNodes.Count > 0 &&
                StructuralTrackPolicy.IsTrackObject(__instance, objId))
            {
                state.FailedNodes.Clear();
            }
        }

        private static bool NormalizeNodesCore(PlanetFactory factory, List<Node> nodes)
        {
            var paths = new HashSet<int>();
            int firstEntity = 0;
            bool assignable = true;
            bool changed = false;
            foreach (Node node in nodes)
            {
                if (firstEntity == 0 && node.ObjectId > 0)
                {
                    firstEntity = node.ObjectId;
                }
                if (node.BeltId > 0)
                {
                    paths.Add(factory.cargoTraffic.beltPool[node.BeltId].segPathId);
                }
                assignable &= node.Assignable;
                changed |= !node.Canonical;
            }
            if (!StructuralTrackCargo.CanNormalize(factory.cargoTraffic, paths))
            {
                return false;
            }
            if (!changed)
            {
                return true;
            }

            normalizationDepth++;
            try
            {
                foreach (Node node in nodes)
                {
                    if (node.ExternalPorts != 0)
                    {
                        DisconnectExternal(factory, node);
                    }
                }
                if (!assignable)
                {
                    foreach (Node node in nodes)
                    {
                        if (node.BeltId > 0)
                        {
                            BeltComponent belt = factory.cargoTraffic.beltPool[node.BeltId];
                            factory.cargoTraffic.AlterBeltConnections(node.BeltId, belt.outputId,
                                belt.rightInputId, belt.backInputId, belt.leftInputId);
                        }
                    }
                    global::ProjectOrbitalRing.ProjectOrbitalRing.logger?.LogWarning(
                        "Orbital track component has multiple neighbors in one longitude direction: planet=" +
                        factory.planetId + ", node=" + nodes[0].ObjectId);
                    return false;
                }
                // Native path segments and reciprocal object ports are rebuilt from the same adjacency snapshot.
                foreach (Node node in nodes)
                {
                    if (node.BeltId > 0)
                    {
                        factory.cargoTraffic.AlterBeltConnections(node.BeltId, 0, 0, 0, 0);
                    }
                }
                foreach (Node node in nodes)
                {
                    factory.ClearObjectConn(node.ObjectId);
                    AlignObject(factory, node.ObjectId);
                }
                foreach (Node node in nodes)
                {
                    if (node.Next != 0)
                    {
                        factory.WriteObjectConn(node.ObjectId, 0, true, node.Next, 1);
                    }
                }
                foreach (Node node in nodes)
                {
                    if (node.BeltId > 0)
                    {
                        factory.cargoTraffic.AlterBeltConnections(node.BeltId,
                            BeltId(factory, node.Next), 0, BeltId(factory, node.Previous), 0);
                    }
                }
            }
            finally
            {
                normalizationDepth--;
            }
            if (firstEntity > 0)
            {
                OrbitalConnect.OnBeltBuiltPostPatch(ref factory, firstEntity);
            }
            return true;
        }

        private static void DisconnectExternal(PlanetFactory factory, Node node)
        {
            for (int slot = 0; slot < 16; slot++)
            {
                if ((node.ExternalPorts & (1 << slot)) == 0)
                {
                    continue;
                }
                factory.ReadObjectConn(node.ObjectId, slot, out _, out int other, out int otherSlot);
                if (other == 0)
                {
                    continue;
                }
                if (node.ObjectId > 0 && other > 0)
                {
                    factory.ApplyEntityDisconnection(other, node.ObjectId, otherSlot, slot);
                }
                factory.ClearObjectConn(node.ObjectId, slot);
            }
        }

        private static Vector3 Position(PlanetFactory factory, int objectId) => objectId > 0
            ? factory.entityPool[objectId].pos : factory.prebuildPool[-objectId].pos;

        private static bool TryGetDirection(Vector3 first, Vector3 last, out bool forward)
        {
            float delta = Mathf.DeltaAngle(Mathf.Atan2(first.x, first.z) * Mathf.Rad2Deg,
                Mathf.Atan2(last.x, last.z) * Mathf.Rad2Deg);
            forward = delta > 0;
            return Mathf.Abs(delta) >= 0.00001f;
        }

        private static void AlignObject(PlanetFactory factory, int objectId)
        {
            Quaternion rotation = TrackRotation(Position(factory, objectId), Quaternion.identity, Vector3.zero);
            if (objectId > 0)
            {
                factory.entityPool[objectId].rot = rotation;
                factory.entityPool[objectId].tilt = 0;
            }
            else
            {
                factory.prebuildPool[-objectId].rot = rotation;
                factory.prebuildPool[-objectId].rot2 = rotation;
                factory.prebuildPool[-objectId].tilt = 0;
                if (factory.planet.physics != null)
                {
                    factory.PostRefreshPrebuildDisplay(-objectId);
                }
            }
        }

        private static void NormalizePreviewGraph(PlanetFactory factory, IReadOnlyList<BuildPreview> previews,
            int count, bool reciprocalPorts)
        {
            if (factory == null)
            {
                return;
            }
            previewLinks.Clear();
            previewPorts.Clear();
            for (int index = 0; index < count; index++)
            {
                BuildPreview preview = previews[index];
                if (preview?.item == null)
                {
                    continue;
                }
                if (!StructuralTrackPolicy.IsTrackItem(preview.item.ID))
                {
                    if (TargetsStructuralTrack(factory, preview.input, preview.inputObjId) ||
                        TargetsStructuralTrack(factory, preview.output, preview.outputObjId))
                    {
                        preview.condition = EBuildCondition.InputConflict;
                    }
                    continue;
                }

                if (preview.input != null || preview.inputObjId != 0)
                {
                    previewLinks.Add((preview, preview.input, preview.inputObjId));
                }
                if (preview.output != null || preview.outputObjId != 0)
                {
                    previewLinks.Add((preview, preview.output, preview.outputObjId));
                }
                preview.input = null;
                preview.output = null;
                preview.inputObjId = 0;
                preview.outputObjId = 0;
                preview.inputToSlot = 1;
                preview.inputFromSlot = 0;
                preview.outputFromSlot = 0;
                preview.outputToSlot = 1;
                preview.inputOffset = 0;
                preview.outputOffset = 0;
                preview.tilt = 0;
                preview.lrot = TrackRotation(preview.lpos, preview.lrot, Vector3.zero);
                preview.lrot2 = preview.lrot;
            }
            foreach (var link in previewLinks)
            {
                ConnectPreview(factory, link.Preview, link.Other, link.OtherObject);
            }

            // Blueprint validation rebuilds reciprocal inputs from outputs; drag previews retain their declared ends.
            foreach (var entry in previewPorts)
            {
                var (preview, forward) = entry.Key;
                var (target, objectId, declared) = entry.Value;
                if (!declared && (!reciprocalPorts || preview.condition == EBuildCondition.InputConflict ||
                    preview.condition == EBuildCondition.Occupied))
                {
                    continue;
                }
                if (reciprocalPorts && target != null)
                {
                    objectId = 0;
                }
                if (forward)
                {
                    preview.output = target;
                    preview.outputObjId = objectId;
                }
                else
                {
                    preview.input = target;
                    preview.inputObjId = objectId;
                }
            }
            previewLinks.Clear();
            previewPorts.Clear();
        }

        private static bool TargetsStructuralTrack(PlanetFactory factory, BuildPreview target, int objectId)
        {
            return target?.item != null
                ? StructuralTrackPolicy.IsTrackItem(target.item.ID)
                : StructuralTrackPolicy.IsTrackObject(factory, objectId);
        }

        private static void ConnectPreview(PlanetFactory factory, BuildPreview preview,
            BuildPreview other, int otherObject)
        {
            int otherItem;
            Vector3 otherPosition;
            if (other != null)
            {
                otherItem = other.item?.ID ?? 0;
                otherPosition = other.lpos;
            }
            else if (!StructuralTrackPolicy.TryReadObject(factory, otherObject, out otherItem, out otherPosition))
            {
                preview.condition = EBuildCondition.InputConflict;
                return;
            }
            if (!StructuralTrackPolicy.CanConnectPreview(factory, preview.item.ID, preview.lpos,
                otherItem, otherPosition))
            {
                preview.condition = EBuildCondition.InputConflict;
                return;
            }
            if (!TryGetDirection(preview.lpos, otherPosition, out bool forward))
            {
                return;
            }
            int targetObject = other?.coverObjId ?? otherObject;
            if (preview.coverObjId != 0 && !CanUseSlot(factory, preview.coverObjId, targetObject, forward ? 0 : 1))
            {
                preview.condition = EBuildCondition.Occupied;
                return;
            }
            if (otherObject != 0 && !CanUseSlot(factory, otherObject, preview.coverObjId, forward ? 1 : 0))
            {
                preview.condition = EBuildCondition.Occupied;
                return;
            }
            if (!PreviewPortAvailable(preview, forward, other, otherObject, true) ||
                (other != null && !PreviewPortAvailable(other, !forward, preview, 0, false)))
            {
                preview.condition = EBuildCondition.InputConflict;
                return;
            }

            previewPorts[(preview, forward)] = (other, otherObject, true);
            if (other != null && !previewPorts.ContainsKey((other, !forward)))
            {
                previewPorts.Add((other, !forward), (preview, 0, false));
            }
        }

        private static bool PreviewPortAvailable(BuildPreview preview, bool forward,
            BuildPreview target, int objectId, bool declared)
        {
            if (!previewPorts.TryGetValue((preview, forward), out var occupied))
            {
                return true;
            }
            int existingObject = occupied.Target?.coverObjId ?? occupied.ObjectId;
            int targetObject = target?.coverObjId ?? objectId;
            if ((occupied.Target == target && occupied.ObjectId == objectId) ||
                (targetObject != 0 && existingObject == targetObject && (!declared || !occupied.Declared)))
            {
                return true;
            }
            preview.condition = EBuildCondition.InputConflict;
            return false;
        }

        private static bool CanUseSlot(PlanetFactory factory, int objectId, int other, int slot)
        {
            int neighbors = 0;
            bool containsOther = false;
            for (int index = 0; index < 4; index++)
            {
                factory.ReadObjectConn(objectId, index, out _, out int neighbor, out _);
                if (!StructuralTrackPolicy.IsTrackObject(factory, neighbor))
                {
                    continue;
                }
                neighbors++;
                containsOther |= neighbor == other || ReplacesPrebuild(factory, neighbor, other);
            }
            if (neighbors >= 2 && !containsOther)
            {
                return false;
            }
            factory.ReadObjectConn(objectId, slot, out _, out int occupied, out _);
            if (occupied == 0 || (other != 0 && occupied == other))
            {
                return true;
            }
            // Drone completion replaces a prebuild reference with the entity at the same placement.
            return ReplacesPrebuild(factory, occupied, other);
        }

        private static bool ReplacesPrebuild(PlanetFactory factory, int prebuild, int entity)
        {
            return prebuild < 0 && entity > 0 && StructuralTrackPolicy.IsTrackObject(factory, prebuild) &&
                StructuralTrackPolicy.IsTrackObject(factory, entity) &&
                factory.prebuildPool[-prebuild].protoId == factory.entityPool[entity].protoId &&
                (Position(factory, prebuild) - Position(factory, entity)).sqrMagnitude < 0.000001f;
        }

        // The expanded connection pool writes ports inside its own prefix.
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConn))]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConnDirect))]
        private static bool CanonicalConnection(PlanetFactory __instance, int objId, ref int slot,
            ref bool isOutput, int otherObjId, ref int otherSlot)
        {
            if (!StructuralTrackPolicy.Enabled)
            {
                return true;
            }
            if (!StructuralTrackPolicy.CanConnect(__instance, objId, otherObjId, out Vector3 position,
                out Vector3 otherPosition, out bool structural))
            {
                return false;
            }
            if (!structural || normalizationDepth > 0)
            {
                return true;
            }
            if (objId == otherObjId || !TryGetDirection(position, otherPosition, out bool forward))
            {
                return false;
            }
            isOutput = forward;
            slot = isOutput ? 0 : 1;
            otherSlot = isOutput ? 1 : 0;
            return CanUseSlot(__instance, objId, otherObjId, slot) &&
                CanUseSlot(__instance, otherObjId, objId, otherSlot);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.DeterminePreviews))]
        private static void PreparePathFactory(BuildTool_Path __instance)
        {
            if (StructuralTrackPolicy.Enabled && StructuralTrackPolicy.IsTrackItem(__instance.handItem?.ID ?? 0))
            {
                EnsureFactory(__instance.factory);
                NormalizeEndpoint(__instance.factory, __instance.startObjectId);
                NormalizeEndpoint(__instance.factory, __instance.castObjectId);
            }
        }

        private static void NormalizeEndpoint(PlanetFactory factory, int objectId)
        {
            if (StructuralTrackPolicy.IsTrackObject(factory, objectId) && !IsCanonical(factory, objectId))
            {
                NormalizeComponent(factory, objectId);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
        private static void PreparePath(BuildTool_Path __instance)
        {
            if (StructuralTrackPolicy.Enabled)
            {
                var previews = __instance.buildPreviews;
                NormalizePreviewGraph(__instance.factory, previews, previews.Count, false);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CheckBuildConditions")]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), "CreatePrebuilds")]
        private static void PrepareBlueprint(BuildTool_BlueprintPaste __instance) => NormalizeBlueprint(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(BuildTool_BlueprintPaste), "DeterminePreviews")]
        private static void AlignBlueprint(BuildTool_BlueprintPaste __instance) => NormalizeBlueprint(__instance);

        private static void NormalizeBlueprint(BuildTool_BlueprintPaste tool)
        {
            if (!StructuralTrackPolicy.Enabled || tool.bpPool == null)
            {
                return;
            }
            EnsureFactory(tool.factory);
            int count = Math.Min(tool.bpCursor, tool.bpPool.Length);
            NormalizePreviewGraph(tool.factory, tool.bpPool, count, true);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.OnBeltBuilt))]
        private static void Built(PlanetFactory __instance, int beltEntityId)
        {
            if (StructuralTrackPolicy.Enabled && normalizationDepth == 0 &&
                StructuralTrackPolicy.IsTrackObject(__instance, beltEntityId))
            {
                factories.GetOrCreateValue(__instance).FailedNodes.Clear();
                EnsureFactory(__instance);
                NormalizeEndpoint(__instance, beltEntityId);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.Import))]
        private static void Imported(CargoTraffic __instance)
        {
            if (__instance.factory != null)
            {
                factories.Remove(__instance.factory);
            }
        }
    }
}
