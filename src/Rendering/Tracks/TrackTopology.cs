namespace ProjectOrbitalRing.Rendering
{
    internal static class TrackTopology
    {
        internal static bool IsNode(PlanetFactory factory, int entityId)
        {
            if (factory?.cargoTraffic == null || entityId <= 0 || entityId >= factory.entityPool.Length ||
                factory.entityPool[entityId].id != entityId ||
                !TrackRenderer.IsRegistered(factory.entityPool[entityId].protoId))
            {
                return false;
            }

            int beltId = factory.entityPool[entityId].beltId;
            return beltId > 0 && beltId < factory.cargoTraffic.beltPool.Length &&
                factory.cargoTraffic.beltPool[beltId].id == beltId &&
                factory.cargoTraffic.beltPool[beltId].entityId == entityId;
        }

        internal static bool TryReadLink(PlanetFactory factory, int entityId, int slot,
            out bool output, out int neighbor)
        {
            output = false;
            neighbor = 0;
            if (!IsNode(factory, entityId))
            {
                return false;
            }

            factory.ReadObjectConn(entityId, slot, out output, out int other, out int otherSlot);
            if (!IsNode(factory, other) || otherSlot < 0 || otherSlot >= 4 ||
                factory.entityPool[other].protoId != factory.entityPool[entityId].protoId)
            {
                return false;
            }

            factory.ReadObjectConn(other, otherSlot, out bool reverseOutput, out int reverse, out _);
            if (reverse != entityId || reverseOutput == output)
            {
                return false;
            }

            neighbor = other;
            return true;
        }
    }
}
