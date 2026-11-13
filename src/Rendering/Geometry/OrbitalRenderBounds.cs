using UnityEngine;

namespace ProjectOrbitalRing.Rendering
{
    internal static class OrbitalRenderBounds
    {
        private static readonly Plane[] planes = new Plane[6];
        private static Camera preparedCamera;

        internal static void Prepare(Camera camera)
        {
            preparedCamera = camera;
            if (camera != null)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, planes);
            }
        }

        internal static bool IsVisible(Camera camera, Bounds bounds, float planetRadius)
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

            float objectRadius = bounds.extents.magnitude;
            float solidRadius = planetRadius - objectRadius;
            if (solidRadius <= 0)
            {
                return true;
            }

            // Orbital meshes and camera positions share a planet-centered coordinate system.
            if (camera.orthographic)
            {
                Vector3 forward = camera.transform.forward;
                float depth = Vector3.Dot(bounds.center, forward);
                bool occluded = depth > objectRadius &&
                    (bounds.center - forward * depth).sqrMagnitude < solidRadius * solidRadius;
                return !occluded;
            }

            Vector3 position = camera.transform.position;
            Vector3 line = bounds.center - position;
            float lengthSquared = line.sqrMagnitude;
            if (lengthSquared < 0.0001f)
            {
                return true;
            }

            float fraction = -Vector3.Dot(position, line) / lengthSquared;
            return !(fraction > 0 && fraction < 1 &&
                (position + line * fraction).sqrMagnitude < solidRadius * solidRadius);
        }
    }
}
