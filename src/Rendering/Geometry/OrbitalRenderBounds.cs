using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalRenderBounds
    {
        private static readonly Plane[] planes = new Plane[6];
        private static Camera preparedCamera;
        private static bool orthographic;
        private static Vector3 cameraDirection;
        private static Vector3 cameraPosition;

        internal static void Prepare(Camera camera)
        {
            preparedCamera = camera;
            if (camera != null)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, planes);
                orthographic = camera.orthographic;
                if (orthographic)
                {
                    cameraDirection = camera.transform.forward;
                }
                else
                {
                    cameraPosition = camera.transform.position;
                }
            }
        }

        internal static bool IsVisible(Camera camera, Bounds bounds, float planetRadius) =>
            TestBounds(camera, bounds, planetRadius, false);

        internal static bool IsDistantTrackVisible(Camera camera, Bounds bounds, float planetRadius) =>
            TestBounds(camera, bounds, planetRadius, true);

        private static bool TestBounds(Camera camera, Bounds bounds, float planetRadius, bool testCorners)
        {
            if (camera == null)
            {
                return false;
            }

            if (preparedCamera != camera)
            {
                Prepare(camera);
            }

            if (!GeometryUtility.TestPlanesAABB(planes, bounds))
            {
                return false;
            }

            float radiusSquared = planetRadius * planetRadius;
            if (planetRadius <= 0 || (testCorners && !orthographic && cameraPosition.sqrMagnitude <= radiusSquared))
            {
                return true;
            }

            float objectRadius = bounds.extents.magnitude;
            float solidRadius = planetRadius - objectRadius;
            if (solidRadius > 0 && IsOccluded(bounds.center, objectRadius, solidRadius * solidRadius))
            {
                return false;
            }
            if (!testCorners)
            {
                return true;
            }

            // The planet's occlusion volume is convex, so all corners must be hidden to reject the box.
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                var position = new Vector3((corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y, (corner & 4) == 0 ? min.z : max.z);
                if (!IsOccluded(position, 0, radiusSquared))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsOccluded(Vector3 center, float objectRadius, float radiusSquared)
        {
            // Orbital meshes and camera positions share a planet-centered coordinate system.
            if (orthographic)
            {
                float depth = Vector3.Dot(center, cameraDirection);
                return depth > objectRadius &&
                    (center - cameraDirection * depth).sqrMagnitude < radiusSquared;
            }

            Vector3 line = center - cameraPosition;
            float lengthSquared = line.sqrMagnitude;
            if (lengthSquared < 0.0001f)
            {
                return false;
            }

            float fraction = -Vector3.Dot(cameraPosition, line) / lengthSquared;
            return fraction > 0 && fraction < 1 &&
                (cameraPosition + line * fraction).sqrMagnitude < radiusSquared;
        }
    }
}
