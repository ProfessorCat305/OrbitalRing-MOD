using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectOrbitalRing.Rendering
{
    internal static partial class TrackRenderer
    {
        private sealed class SelectionMesh
        {
            internal NodeShape Shape;
            internal StructureLodMeshes Meshes;
        }

        private static PlanetFactory selectionFactory;
        private static readonly Dictionary<int, int> selectionStates = new Dictionary<int, int>();
        private static readonly Dictionary<int, SelectionMesh> selectionMeshes = new Dictionary<int, SelectionMesh>();
        private static readonly List<int> expiredSelections = new List<int>();
        private static readonly MaterialPropertyBlock selectionProperties = new MaterialPropertyBlock();
        private static Material selectionMaterial;
        private static int selectionBuildFrame = -1;

        internal static void SetSelection(PlanetFactory factory, int entityId, int state)
        {
            if (selectionFactory != factory)
            {
                ResetSelection();
                selectionFactory = factory;
            }
            if (state == 0)
            {
                ForgetSelection(factory, entityId);
            }
            else
            {
                selectionStates[entityId] = state;
            }
        }

        internal static void ClearSelection(PlanetFactory factory)
        {
            if (selectionFactory == factory)
            {
                // Selection follows the native logic phase, including paused and multi-tick frames.
                selectionStates.Clear();
            }
        }

        internal static void ForgetSelection(PlanetFactory factory, int entityId)
        {
            if (selectionFactory != factory)
            {
                return;
            }
            selectionStates.Remove(entityId);
            if (selectionMeshes.TryGetValue(entityId, out SelectionMesh cached))
            {
                DestroyDetailMeshes(cached.Meshes);
                selectionMeshes.Remove(entityId);
            }
        }

        private static void DrawSelection(PlanetFactory factory, Camera camera, int itemId)
        {
            if (selectionFactory != factory)
            {
                return;
            }
            expiredSelections.Clear();
            foreach (var entry in selectionMeshes)
            {
                if (!selectionStates.ContainsKey(entry.Key) || !TrackTopology.IsNode(factory, entry.Key))
                {
                    expiredSelections.Add(entry.Key);
                }
            }
            foreach (int entityId in expiredSelections)
            {
                ForgetSelection(factory, entityId);
            }

            foreach (var entry in selectionStates)
            {
                int entityId = entry.Key;
                if (!TrackTopology.IsNode(factory, entityId))
                {
                    continue;
                }
                NodeShape shape = GetNodeShape(factory, entityId);
                if (itemId > 0 && shape.ItemId != itemId)
                {
                    continue;
                }
                Vector3 origin = Origin(shape.Key);
                var definition = GetDefinition(shape.ItemId);
                var bounds = new Bounds(shape.Position + shape.Center, Vector3.one * definition.Radius * 2);
                foreach (Vector3 end in shape.Ends)
                {
                    bounds.Encapsulate(shape.Position + end);
                }
                bounds.Expand(definition.Radius * 2);
                if (!OrbitalRenderBounds.IsVisible(camera, bounds, factory.planet.realRadius - 2))
                {
                    continue;
                }

                if (!selectionMeshes.TryGetValue(entityId, out SelectionMesh cached) || cached.Shape != shape)
                {
                    // The first overlay stays responsive while bulk selections share the mesh build budget.
                    if (!OrbitalMeshBuildBudget.HasBuildBudget && selectionBuildFrame == Time.frameCount)
                    {
                        continue;
                    }
                    selectionBuildFrame = Time.frameCount;
                    long started = Stopwatch.GetTimestamp();
                    try
                    {
                        if (cached != null)
                        {
                            DestroyDetailMeshes(cached.Meshes);
                            selectionMeshes.Remove(entityId);
                        }
                        cached = new SelectionMesh
                        {
                            Shape = shape,
                            Meshes = BuildSelectionMesh(factory, entityId, shape),
                        };
                        selectionMeshes.Add(entityId, cached);
                    }
                    finally
                    {
                        OrbitalMeshBuildBudget.RecordBuildTime(started);
                    }
                }
                if (selectionMaterial == null)
                {
                    selectionMaterial = new Material(Configs.builtin.previewGizmoMat)
                    {
                        name = "OrbitalRing.TrackSelection",
                    };
                    selectionMaterial.DisableKeyword("_ENABLE_VFINST");
                    selectionMaterial.SetFloat("_ToggleVerta", 0);
                }
                int speed = factory.cargoTraffic.beltPool[factory.entityPool[entityId].beltId].speed;
                // Blueprint copy uses reserved preselect/delete codes and ten times the belt speed for selection.
                Color color = entry.Value == 1000 ? Configs.builtin.copyDeleteColor
                    : entry.Value == 100 ? Configs.builtin.copyPreselectColor
                    : entry.Value == speed * 10 ? Configs.builtin.copySelectedColor : (Color32)Color.white;
                color.a = 0.45f;
                selectionProperties.SetColor("_Color", color);
                selectionProperties.SetColor("_NotVisibleColor", color);
                selectionProperties.SetColor("_HoloColor", color);
                Matrix4x4 transform = Matrix4x4.Translate(origin);
                Submit(cached.Meshes.Wall, 0);
                Submit(cached.Meshes.Core, 0);
                Submit(cached.Meshes.Core, 1);
                Submit(cached.Meshes.Core, 4);

                void Submit(Mesh mesh, int submesh)
                {
                    if (mesh != null && mesh.GetIndexCount(submesh) > 0)
                    {
                        Graphics.DrawMesh(mesh, transform, selectionMaterial, 0, camera, submesh,
                            selectionProperties, ShadowCastingMode.Off, false);
                    }
                }
            }
        }

        private static StructureLodMeshes BuildSelectionMesh(PlanetFactory factory, int entityId, NodeShape shape)
        {
            var core = new TrackMesh();
            var definition = GetDefinition(shape.ItemId);
            var walls = definition.HasGlassWall ? new TrackMesh(definition.WallRadius, Origin(shape.Key), 1) : null;
            var meshes = new StructureLodMeshes();
            try
            {
                // Per-node overlays reuse the track envelope without invalidating shared chunk meshes.
                foreach (var step in AppendNodeModel(factory, shape.Key, core, walls, entityId, 1)) { }
                meshes.Core = core.VertexCount > 0 ? core.Build("OrbitalRing.TrackSelection.Core") : null;
                meshes.Wall = walls?.VertexCount > 0 ? walls.Build("OrbitalRing.TrackSelection.Wall") : null;
                return meshes;
            }
            catch
            {
                DestroyDetailMeshes(meshes);
                throw;
            }
        }

        private static void ClearSelectionMeshes()
        {
            foreach (SelectionMesh cached in selectionMeshes.Values)
            {
                DestroyDetailMeshes(cached.Meshes);
            }
            selectionMeshes.Clear();
        }

        private static void ResetSelection()
        {
            ClearSelectionMeshes();
            selectionStates.Clear();
            selectionFactory = null;
        }
    }
}
