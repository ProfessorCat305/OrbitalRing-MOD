using UnityEngine;
using static ProjectOrbitalRing.Rendering.AirBeltAppearance;

namespace ProjectOrbitalRing.Rendering
{
    internal static partial class AirBeltPowerRenderer
    {
        private static bool NearJunction(CargoTraffic traffic, int beltId, Vector3 position)
        {
            if (beltId <= 0 || beltId >= traffic.beltPool.Length || traffic.beltPool[beltId].id != beltId)
            {
                return false;
            }

            BeltComponent belt = traffic.beltPool[beltId];
            int inputs = (belt.backInputId > 0 ? 1 : 0) + (belt.leftInputId > 0 ? 1 : 0) +
                (belt.rightInputId > 0 ? 1 : 0);
            return inputs > 1 && (traffic.factory.entityPool[belt.entityId].pos - position).sqrMagnitude <
                JunctionClearance * JunctionClearance;
        }

        private static bool NeedsJunctionClearance(CargoTraffic traffic, BeltComponent belt, Vector3 position) =>
            NearJunction(traffic, belt.id, position) || NearJunction(traffic, belt.outputId, position) ||
            NearJunction(traffic, belt.backInputId, position) || NearJunction(traffic, belt.leftInputId, position) ||
            NearJunction(traffic, belt.rightInputId, position);

        private static float Coordinate(Vector3 value, int axis) =>
            axis == 0 ? value.x : axis == 1 ? value.y : value.z;

        private static bool TryAnchorSpan(CargoTraffic traffic, int beltId, out BeltRenderingBatch batch,
            out int first, out int last)
        {
            BeltComponent belt = IsAirBelt(traffic, beltId) ? traffic.beltPool[beltId] : default;
            return TryAnchorSpan(traffic, belt, out batch, out first, out last);
        }

        private static bool TryAnchorSpan(CargoTraffic traffic, BeltComponent belt, out BeltRenderingBatch batch,
            out int first, out int last)
        {
            var batches = renderingBatches(traffic);
            int index = belt.modelBatchIndex - 1;
            batch = batches != null && index >= 0 && index < batches.Length ? batches[index] : null;
            first = belt.modelIndex;
            last = batch == null ? first : first + batch.nodeWidth - 1;
            return last > first && first >= 0 && last < batch.nodePool.Length;
        }

        private static void FindSupports(CargoTraffic traffic, BeltComponent belt, CargoPath path,
            SupportGroup supports, BeltRenderingBatch batch, int first, int last)
        {
            int airInputs = (IsAirBelt(traffic, belt.backInputId) ? 1 : 0) +
                (IsAirBelt(traffic, belt.leftInputId) ? 1 : 0) +
                (IsAirBelt(traffic, belt.rightInputId) ? 1 : 0);
            if (airInputs > 1 && TryRenderedPoint(batch, first, last, belt.segIndex + belt.segPivotOffset,
                out Support intersection, out _))
            {
                int[] neighbors = { belt.backInputId, belt.leftInputId, belt.rightInputId, belt.outputId };
                float bestClearance = -1;
                Quaternion bestRotation = intersection.Rotation;
                for (int step = 0; step < 16; step++)
                {
                    Quaternion rotation = intersection.Rotation * Quaternion.Euler(0, step * 22.5f, 0);
                    Vector3 direction = rotation * Vector3.right;
                    float clearance = 1;
                    foreach (int neighbor in neighbors)
                    {
                        if (!IsBelt(traffic, neighbor))
                        {
                            continue;
                        }
                        Vector3 branch = BeltPosition(traffic, neighbor) - intersection.Position;
                        branch = Vector3.ProjectOnPlane(branch, intersection.Position.normalized).normalized;
                        float along = Mathf.Max(0, Vector3.Dot(direction, branch));
                        clearance = Mathf.Min(clearance, 1 - along * along);
                    }
                    if (clearance > bestClearance)
                    {
                        bestClearance = clearance;
                        bestRotation = rotation;
                    }
                }
                supports.Add(2, new Support(intersection.Position, bestRotation), 0.001f);
                return;
            }

            if (HasHead(traffic, belt, path))
            {
                BeltAnchor anchor = batch.nodePool[first];
                supports.Add(0, new Support(anchor.pos, anchor.rot), 0.001f);
            }

            if (HasTail(traffic, belt, path))
            {
                BeltAnchor anchor = batch.nodePool[last];
                supports.Add(1, new Support(anchor.pos, anchor.rot), 0.001f);
            }

            if (TryCornerSupport(traffic, belt.id, out Support corner, out float angle, out bool gradeBreak))
            {
                float clearance = gradeBreak ? GradeSupportSpacing : CornerSpacing;
                if (!NearFreeEnd(path, corner.Position, clearance) &&
                    !NearTerminal(traffic, belt.mainInputId, corner.Position, clearance) &&
                    !NearTerminal(traffic, belt.outputId, corner.Position, clearance) &&
                    !PreferCorner(traffic, belt.mainInputId, belt.id, corner, angle, gradeBreak) &&
                    !PreferCorner(traffic, belt.outputId, belt.id, corner, angle, gradeBreak))
                {
                    supports.Add(2, corner, clearance);
                }
            }

            if (TryGridSupport(traffic, belt, batch, first, last, out Support grid) &&
                !NearFreeEnd(path, grid.Position, CornerSpacing) &&
                !NearCorner(traffic, belt.mainInputId, grid.Position) &&
                !NearCorner(traffic, belt.outputId, grid.Position) &&
                !NearTerminal(traffic, belt.mainInputId, grid.Position, CornerSpacing) &&
                !NearTerminal(traffic, belt.outputId, grid.Position, CornerSpacing))
            {
                supports.Add(3, grid, CornerSpacing);
            }

        }

        private static bool HasHead(CargoTraffic traffic, BeltComponent belt, CargoPath path) =>
            !path.closed && belt.segIndex == 0 || belt.mainInputId > 0 && !IsAirBelt(traffic, belt.mainInputId);

        private static Vector3 BeltPosition(CargoTraffic traffic, int beltId) =>
            traffic.factory.entityPool[traffic.beltPool[beltId].entityId].pos;

        private static bool HasTail(CargoTraffic traffic, BeltComponent belt, CargoPath path) =>
            !path.closed && path.outputPath == null && belt.segIndex + belt.segLength == path.pathLength ||
            belt.outputId > 0 && !IsAirBelt(traffic, belt.outputId);

        private static bool NearTerminal(CargoTraffic traffic, int beltId, Vector3 position, float clearance)
        {
            if (!TryAnchorSpan(traffic, beltId, out BeltRenderingBatch batch, out int first, out int last))
            {
                return false;
            }

            BeltComponent belt = traffic.beltPool[beltId];
            CargoPath path = traffic.GetCargoPath(belt.segPathId);
            if (path == null)
            {
                return false;
            }

            return HasHead(traffic, belt, path) &&
                (batch.nodePool[first].pos - position).sqrMagnitude < clearance * clearance ||
                HasTail(traffic, belt, path) &&
                (batch.nodePool[last].pos - position).sqrMagnitude < clearance * clearance;
        }

        private static bool TryGridSupport(CargoTraffic traffic, BeltComponent belt, BeltRenderingBatch batch,
            int first, int last, out Support support)
        {
            support = default;
            Vector3 tangent = batch.nodePool[last].pos - batch.nodePool[first].pos;
            Vector3 magnitude = new Vector3(Mathf.Abs(tangent.x), Mathf.Abs(tangent.y), Mathf.Abs(tangent.z));
            int axis = magnitude.x >= magnitude.y && magnitude.x >= magnitude.z ? 0 :
                magnitude.y >= magnitude.z ? 1 : 2;
            for (int index = first; index < last; index++)
            {
                BeltAnchor start = batch.nodePool[index];
                BeltAnchor end = batch.nodePool[index + 1];
                float from = Coordinate(start.pos, axis);
                float to = Coordinate(end.pos, axis);
                if (Mathf.Abs(to - from) < 0.000001f)
                {
                    continue;
                }

                // World grid planes keep support placement independent of path splits and extension order.
                float plane = to > from ? (Mathf.Floor(from / SupportSpacing) + 1) * SupportSpacing :
                    (Mathf.Ceil(from / SupportSpacing) - 1) * SupportSpacing;
                float fraction = (plane - from) / (to - from);
                if (fraction > 0.00001f && fraction <= 1.00001f)
                {
                    fraction = Mathf.Min(fraction, 1);
                    support = new Support(Vector3.Lerp(start.pos, end.pos, fraction),
                        Quaternion.Slerp(start.rot, end.rot, fraction));
                    return !NeedsJunctionClearance(traffic, belt, support.Position);
                }
            }

            return false;
        }

        private static bool TryCornerSupport(CargoTraffic traffic, int beltId, out Support support, out float angle,
            out bool gradeBreak)
        {
            support = default;
            angle = 0;
            gradeBreak = false;
            if (!TryAnchorSpan(traffic, beltId, out BeltRenderingBatch batch, out int first, out int last))
            {
                return false;
            }

            BeltAnchor start = batch.nodePool[first];
            BeltAnchor end = batch.nodePool[last];
            float gradeChange = LocalGradeChange(traffic, beltId, start, end);
            gradeBreak = gradeChange >= GradeBreakAngle;
            Vector3 up = (start.pos + end.pos).normalized;
            Vector3 incoming = Vector3.ProjectOnPlane(start.rot * Vector3.forward, up);
            Vector3 outgoing = Vector3.ProjectOnPlane(end.rot * Vector3.forward, up);
            float yaw = 0;
            if (incoming.sqrMagnitude >= 0.01f && outgoing.sqrMagnitude >= 0.01f)
            {
                yaw = Vector3.Angle(incoming, outgoing);
            }

            if (!gradeBreak && yaw < CornerAngle)
            {
                return false;
            }

            angle = Mathf.Max(yaw, gradeChange);
            int middle = (first + last) / 2;
            float fraction = (first + last) * 0.5f - middle;
            BeltAnchor before = batch.nodePool[middle];
            BeltAnchor after = batch.nodePool[Mathf.Min(middle + 1, last)];
            support = new Support(Vector3.Lerp(before.pos, after.pos, fraction),
                Quaternion.Slerp(before.rot, after.rot, fraction));
            return !NeedsJunctionClearance(traffic, traffic.beltPool[beltId], support.Position);
        }

        private static float SignedGrade(BeltAnchor anchor)
        {
            // Each radial frame removes the planet's curvature from the change in slope.
            float rise = Vector3.Dot(anchor.rot * Vector3.forward, anchor.pos.normalized);
            return Mathf.Asin(Mathf.Clamp(rise, -1, 1)) * Mathf.Rad2Deg;
        }

        private static float LocalGradeChange(CargoTraffic traffic, int beltId, BeltAnchor start, BeltAnchor end)
        {
            float incoming = SignedGrade(start);
            float outgoing = SignedGrade(end);
            float ownChange = Mathf.Abs(outgoing - incoming);
            BeltComponent belt = traffic.beltPool[beltId];
            if (TryAnchorSpan(traffic, belt.mainInputId, out BeltRenderingBatch previous,
                out int previousFirst, out int previousLast) &&
                (previous.nodePool[previousLast].pos - start.pos).sqrMagnitude < 0.000001f)
            {
                incoming = SignedGrade(previous.nodePool[previousFirst]);
            }

            if (TryAnchorSpan(traffic, belt.outputId, out BeltRenderingBatch next,
                out int nextFirst, out int nextLast) &&
                (next.nodePool[nextFirst].pos - end.pos).sqrMagnitude < 0.000001f)
            {
                outgoing = SignedGrade(next.nodePool[nextLast]);
            }

            float localChange = Mathf.Abs(outgoing - incoming);
            // Native smoothing spreads a grade break across adjoining spans; constant-grade spans stay excluded.
            return ownChange >= localChange * 0.25f ? Mathf.Max(ownChange, localChange) : ownChange;
        }

        private static bool PreferCorner(CargoTraffic traffic, int neighborId, int beltId, Support support,
            float angle, bool gradeBreak)
        {
            if (!TryCornerSupport(traffic, neighborId, out Support neighbor, out float neighborAngle,
                out bool neighborGradeBreak))
            {
                return false;
            }

            float clearance = gradeBreak == neighborGradeBreak ? CornerSpacing : GradeSupportSpacing;
            if ((neighbor.Position - support.Position).sqrMagnitude >= clearance * clearance)
            {
                return false;
            }

            return neighborAngle > angle + 0.5f || Mathf.Abs(neighborAngle - angle) <= 0.5f && neighborId < beltId;
        }

        private static bool NearCorner(CargoTraffic traffic, int beltId, Vector3 position) =>
            TryCornerSupport(traffic, beltId, out Support corner, out _, out _) &&
            (corner.Position - position).sqrMagnitude < CornerSpacing * CornerSpacing;

        private static bool NearFreeEnd(CargoPath path, Vector3 position, float clearance)
        {
            if (path.closed || path.pathLength <= 5)
            {
                return false;
            }

            return (path.pointPos[4] - position).sqrMagnitude < clearance * clearance ||
                path.outputPath == null && (path.pointPos[path.pathLength - 6] - position).sqrMagnitude <
                clearance * clearance;
        }

        private static Junction FindJunction(CargoTraffic traffic, int beltId)
        {
            if (!IsBelt(traffic, beltId))
            {
                return null;
            }

            BeltComponent belt = traffic.beltPool[beltId];
            int outputId = belt.outputId;
            bool sourceIsAir = IsAirBelt(traffic, beltId);
            bool outputIsAir = IsAirBelt(traffic, outputId);
            if (!IsBelt(traffic, outputId) || (!sourceIsAir && !outputIsAir))
            {
                return null;
            }

            bool straight = traffic.beltPool[outputId].mainInputId == beltId;
            if (straight && sourceIsAir == outputIsAir)
            {
                return null;
            }

            CargoPath path = traffic.GetCargoPath(belt.segPathId);
            BeltComponent output = traffic.beltPool[outputId];
            CargoPath downstream = traffic.GetCargoPath(output.segPathId);
            int pivot = output.segIndex + output.segPivotOffset;
            if (path == null || downstream == null || (path != downstream && path.outputPath != downstream) ||
                pivot < 0 || pivot >= downstream.pathLength ||
                !TryAnchorSpan(traffic, belt, out var source, out _, out int sourceEnd) ||
                !TryAnchorSpan(traffic, output, out var destination, out int first, out int last))
            {
                return null;
            }

            BeltAnchor anchor = source.nodePool[sourceEnd];
            if (!TryRenderedPoint(destination, first, last, pivot, out Support end, out Vector3 wireEnd))
            {
                return null;
            }
            if (straight)
            {
                BeltAnchor start = destination.nodePool[first];
                end = new Support(start.pos, start.rot);
                if ((anchor.pos - start.pos).sqrMagnitude < 0.000004f)
                {
                    return null;
                }
            }

            return new Junction(new Support(anchor.pos, anchor.rot), end, wireEnd, sourceIsAir && outputIsAir);
        }

        private static bool TryRenderedPoint(BeltRenderingBatch batch, int first, int last, int pivot,
            out Support point, out Vector3 wire)
        {
            point = default;
            wire = default;
            var offset = new Vector3(AirBeltMesh.WireSide, AirBeltMesh.WireHeight, 0);
            for (int index = first; index < last; index++)
            {
                BeltAnchor start = batch.nodePool[index];
                BeltAnchor end = batch.nodePool[index + 1];
                if (pivot > end.t && index + 1 < last)
                {
                    continue;
                }

                float fraction = Mathf.InverseLerp(start.t, end.t, pivot);
                point = new Support(Vector3.Lerp(start.pos, end.pos, fraction),
                    Quaternion.Slerp(start.rot, end.rot, fraction));
                // The native shader interpolates transformed section vertices between adjacent anchors.
                wire = Vector3.Lerp(start.pos + start.rot * offset, end.pos + end.rot * offset, fraction);
                return true;
            }

            return false;
        }

    }
}
