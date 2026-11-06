using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProjectOrbitalRing.Patches.Logic.OrbitalRing;
using ProjectOrbitalRing.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using static ProjectOrbitalRing.Patches.Logic.OrbitalRing.PosTool;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalRingBraces
    {
        private const int DetailCount = 3;
        private const int AnchorsPerChunk = 4;

        private struct BraceDetail
        {
            internal Mesh Mesh;
            internal Mesh Lights;
        }

        private sealed class BraceChunk
        {
            internal readonly BraceDetail[] Details = new BraceDetail[DetailCount];
            internal Bounds Bounds;
            internal int DetailLevel = 2;
        }

        private sealed class RingPair
        {
            internal PlanetFactory Factory;
            internal int Ring;
            internal int Band;
            internal readonly BraceDetail[] Details = new BraceDetail[DetailCount];
            internal readonly List<BraceChunk> Chunks = new List<BraceChunk>();
            internal IEnumerator<object> Work;
        }

        private static readonly Dictionary<int, RingPair> pairs = new Dictionary<int, RingPair>();
        private static PlanetFactory observedFactory;
        private static Material metal;
        private static Material signal;
        private static Material simplifiedMetal;
        private static readonly MaterialPropertyBlock lighting = new MaterialPropertyBlock();
        private static readonly MaterialPropertyBlock signalProperties = new MaterialPropertyBlock();
        private static readonly List<BraceChunk> visibleChunks = new List<BraceChunk>();

        private static void SetActive(PlanetFactory factory, int ring, int band, bool active)
        {
            int key = ring * 4 + band;
            if (!active)
            {
                if (pairs.TryGetValue(key, out RingPair pair))
                {
                    Release(pair);
                    pairs.Remove(key);
                }
                return;
            }

            if (!pairs.ContainsKey(key))
            {
                var pair = new RingPair { Factory = factory, Ring = ring, Band = band };
                pair.Work = Build(pair).GetEnumerator();
                pairs.Add(key, pair);
            }
        }

        private static void Synchronize(PlanetFactory factory)
        {
            if (factory != observedFactory)
            {
                Clear();
                observedFactory = factory;
            }
            if (factory == null || !RemodelSettings.RingBraces || !RemodelSettings.Structure)
            {
                return;
            }

            var data = OrbitalStationManager.Instance.GetPlanetOrbitalRingData(factory.planetId);
            if (data == null)
            {
                Clear();
                return;
            }

            for (int index = 0; index < data.Rings.Count; index++)
            {
                EquatorRing ring = data.Rings[index];
                SetActive(factory, index, 1,
                    RemodelSettings.Accelerator && ring.isInsideRingComplete && ring.isLowInsideRingComplete);
                SetActive(factory, index, 2,
                    RemodelSettings.Power && ring.isOutsideRingComplete && ring.isLowOutsideRingComplete);
            }
        }

        internal static void Update(PlanetFactory factory)
        {
            Synchronize(factory);
            long deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 1000;
            foreach (var entry in pairs)
            {
                RingPair pair = entry.Value;
                while (pair.Work != null && OrbitalMeshBuildBudget.HasBuildBudget &&
                    Stopwatch.GetTimestamp() < deadline)
                {
                    long started = Stopwatch.GetTimestamp();
                    try
                    {
                        if (!pair.Work.MoveNext())
                        {
                            pair.Work.Dispose();
                            pair.Work = null;
                        }
                    }
                    catch
                    {
                        pairs.Remove(entry.Key);
                        Release(pair);
                        throw;
                    }
                    finally
                    {
                        OrbitalMeshBuildBudget.RecordBuildTime(started);
                    }
                }
            }
        }

        private static void Release(RingPair pair)
        {
            pair.Work?.Dispose();
            ReleaseDetails(pair.Details);
            foreach (BraceChunk chunk in pair.Chunks)
            {
                ReleaseDetails(chunk.Details);
            }
        }

        private static void ReleaseDetails(BraceDetail[] details)
        {
            foreach (BraceDetail detail in details)
            {
                UnityEngine.Object.Destroy(detail.Mesh);
                UnityEngine.Object.Destroy(detail.Lights);
            }
        }

        internal static void Draw(PlanetFactory factory, Camera camera)
        {
            var model = factory?.planet?.factoryModel;
            if (camera == null || model == null || model.disableFactoryEntities
                || model.gpuiManager == null || !model.gpuiManager.renderEntity)
            {
                return;
            }

            OrbitalRenderBounds.Prepare(camera);
            TrackLighting.Apply(lighting);
            foreach (var pair in pairs.Values)
            {
                if (pair.Factory != factory || pair.Work != null)
                {
                    continue;
                }

                Color theme = pair.Band == 1 ? new Color(0.64f, 0.36f, 0.95f) : new Color(0.10f, 0.90f, 0.28f);
                float breath = 1.1f + Mathf.Sin(Time.time * Mathf.PI * 2 / 3 + pair.Ring * 0.4f) * 0.30f;
                signalProperties.SetColor("_Color", Color.Lerp(theme, Color.white, 0.35f) * 0.4f);
                signalProperties.SetColor("_EmissionColor", theme * breath);
                DrawPair(pair, camera);
            }
        }

        private static void DrawPair(RingPair pair, Camera camera)
        {
            visibleChunks.Clear();
            int commonDetail = -1;
            bool uniformDetail = true;
            Vector3 cameraPosition = camera.transform.position;
            float screenScale = camera.orthographic ? camera.pixelHeight / (2 * camera.orthographicSize)
                : camera.pixelHeight / (2 * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f));
            foreach (BraceChunk chunk in pair.Chunks)
            {
                float distance = Mathf.Sqrt(Mathf.Max(1, chunk.Bounds.SqrDistance(cameraPosition)));
                float collarPixels = 0.085f * screenScale / (camera.orthographic ? 1 : distance);
                chunk.DetailLevel = DetailLevel(collarPixels, chunk.DetailLevel);
                if (OrbitalRenderBounds.IsVisible(camera, chunk.Bounds, pair.Factory.planet.realRadius - 2))
                {
                    if (commonDetail < 0)
                    {
                        commonDetail = chunk.DetailLevel;
                    }
                    uniformDetail &= commonDetail == chunk.DetailLevel;
                    visibleChunks.Add(chunk);
                }
            }

            // A fixed caster keeps ground shadows independent of view culling and detail transitions.
            Graphics.DrawMesh(pair.Details[2].Mesh, Matrix4x4.identity, Metal, 0, camera, 0, lighting,
                ShadowCastingMode.ShadowsOnly, false);
            if (uniformDetail && visibleChunks.Count * 3 > pair.Chunks.Count)
            {
                DrawDetail(pair.Details[commonDetail], commonDetail, camera);
                return;
            }

            foreach (BraceChunk chunk in visibleChunks)
            {
                DrawDetail(chunk.Details[chunk.DetailLevel], chunk.DetailLevel, camera);
            }
        }

        private static int DetailLevel(float collarPixels, int previous)
        {
            if (collarPixels > (previous == 0 ? 2.5f : 3.5f))
            {
                return 0;
            }
            return collarPixels > (previous == 2 ? 1.1f : 0.75f) ? 1 : 2;
        }

        private static void DrawDetail(BraceDetail detail, int detailLevel, Camera camera)
        {
            Graphics.DrawMesh(detail.Mesh, Matrix4x4.identity, detailLevel == 0 ? Metal : SimplifiedMetal,
                0, camera, 0, lighting, ShadowCastingMode.Off, true);
            Graphics.DrawMesh(detail.Lights, Matrix4x4.identity, Signal, 0, camera, 0, signalProperties,
                ShadowCastingMode.Off, false);
        }

        internal static Material Metal
        {
            get
            {
                if (metal == null)
                {
                    metal = TrackModelAssets.CreateMaterial("brace-Metal");
                }
                return metal;
            }
        }

        private static Material SimplifiedMetal
        {
            get
            {
                if (simplifiedMetal == null)
                {
                    simplifiedMetal = TrackModelAssets.CreateMaterial("brace-SimplifiedMetal");
                }
                return simplifiedMetal;
            }
        }

        private static Material Signal
        {
            get
            {
                if (signal == null)
                {
                    signal = TrackModelAssets.CreateMaterial("brace-Signal");
                }
                return signal;
            }
        }

        private static Vector3 Center(RingPair pair, float longitude, bool structure)
        {
            int itemId = structure ? ProtoID.I轨道连接组件 : pair.Band == 1 ? ProtoID.I粒子加速轨道 : ProtoID.I星环电网组件;
            return OrbitalTextureLayout.TrackCenter(pair.Factory, pair.Ring, pair.Band, longitude, itemId);
        }

        private static IEnumerable<object> Build(RingPair pair)
        {
            bool accelerator = pair.Band == 1;
            int functionalItem = accelerator ? ProtoID.I粒子加速轨道 : ProtoID.I星环电网组件;
            var layout = OrbitalTextureLayout.FromBand(pair.Factory.planet, functionalItem, pair.Band,
                accelerator ? 1.28f : 0.98f);
            int anchors = Math.Max(1, Mathf.RoundToInt(layout.RepeatCount / (accelerator ? 10f : 5f)));
            for (int firstAnchor = 0; firstAnchor < anchors; firstAnchor += AnchorsPerChunk)
            {
                var chunk = new BraceChunk();
                pair.Chunks.Add(chunk);
                for (int detailLevel = 0; detailLevel < DetailCount; detailLevel++)
                {
                    var builder = new OrbitalAttachmentMesh(detailLevel);
                    var indicators = new OrbitalAttachmentMesh(detailLevel);
                    int lastAnchor = Math.Min(anchors, firstAnchor + AnchorsPerChunk);
                    for (int index = firstAnchor; index < lastAnchor; index++)
                    {
                        AddAnchor(builder, indicators, pair, index, detailLevel);
                        yield return null;
                    }

                    Mesh metalMesh = builder.Build();
                    chunk.Details[detailLevel].Mesh = metalMesh;
                    metalMesh.name = "OrbitalRing.BraceMetal.Lod" + detailLevel;
                    Mesh lightMesh = indicators.Build();
                    chunk.Details[detailLevel].Lights = lightMesh;
                    lightMesh.name = "OrbitalRing.BraceIndicators.Lod" + detailLevel;
                    if (detailLevel == 0)
                    {
                        chunk.Bounds = metalMesh.bounds;
                        chunk.Bounds.Encapsulate(lightMesh.bounds);
                    }
                    yield return null;
                }
            }

            for (int detailLevel = 0; detailLevel < DetailCount; detailLevel++)
            {
                string name = "OrbitalRing.Braces.Lod" + detailLevel;
                pair.Details[detailLevel].Mesh = OrbitalMeshBuffers.Combine(name + ".Metal",
                    pair.Chunks.Select(chunk => chunk.Details[detailLevel].Mesh));
                yield return null;
                pair.Details[detailLevel].Lights = OrbitalMeshBuffers.Combine(name + ".Lights",
                    pair.Chunks.Select(chunk => chunk.Details[detailLevel].Lights));
                yield return null;
                pair.Details[detailLevel].Mesh.UploadMeshData(true);
                pair.Details[detailLevel].Lights.UploadMeshData(true);
                foreach (BraceChunk chunk in pair.Chunks)
                {
                    chunk.Details[detailLevel].Mesh.UploadMeshData(true);
                    chunk.Details[detailLevel].Lights.UploadMeshData(true);
                    yield return null;
                }
            }
        }

        private static void AddAnchor(OrbitalAttachmentMesh builder, OrbitalAttachmentMesh indicators,
            RingPair pair, int index, int detailLevel)
        {
            bool accelerator = pair.Band == 1;
            float longitude = OrbitalTextureLayout.FacilityLongitude(pair.Factory.planet,
                index * (accelerator ? 1f : 0.5f));
            Vector3 upper = Center(pair, longitude, true);
            Vector3 lower = Center(pair, longitude, false);
            Vector3 up = upper.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, up).normalized;
            Vector3 side = Vector3.Cross(tangent, up).normalized;
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float sign = sideIndex == 0 ? -1 : 1;
                float lateral = accelerator ? 1.29f : 0.64f;
                Vector3 top = upper - up * (accelerator ? 0.80f : 0.655f) + side * (sign * lateral);
                Vector3 bottom = lower + up * (accelerator ? 0 : 0.574f) + side * (sign * lateral);
                AddMount(builder, top, -up, side, tangent, detailLevel);
                AddMount(builder, bottom, accelerator ? side * sign : up,
                    accelerator ? up : side, tangent, detailLevel);
                builder.AddRod("brace-rod", top, bottom);
                Vector3 axis = (bottom - top).normalized;
                Vector3 sleeve = Vector3.Lerp(top, bottom, 0.34f);
                builder.AddRod("brace-sleeve", sleeve - axis * 0.18f, sleeve + axis * 0.18f);
                builder.AddRod("brace-collar", top + axis * 0.09f, top + axis * 0.21f);
                builder.AddRod("brace-collar", bottom - axis * 0.21f, bottom - axis * 0.09f);
                indicators.AddRod("brace-indicator", top + axis * 0.22f, top + axis * 0.29f);
                indicators.AddRod("brace-indicator", bottom - axis * 0.29f, bottom - axis * 0.22f);
            }
        }

        private static void AddMount(OrbitalAttachmentMesh builder, Vector3 anchor, Vector3 normal,
            Vector3 across, Vector3 along, int detailLevel)
        {
            builder.AddPart("brace-mount-lod" + detailLevel, anchor, across, normal, along);
        }

        internal static void Clear()
        {
            foreach (var pair in pairs.Values)
            {
                Release(pair);
            }

            pairs.Clear();
            visibleChunks.Clear();
            observedFactory = null;
        }

        internal static void Dispose()
        {
            Clear();
            if (metal != null)
            {
                UnityEngine.Object.Destroy(metal);
                metal = null;
            }

            if (signal != null)
            {
                UnityEngine.Object.Destroy(signal);
                signal = null;
            }
            if (simplifiedMetal != null)
            {
                UnityEngine.Object.Destroy(simplifiedMetal);
                simplifiedMetal = null;
            }
        }
    }
}
