using System;
using UnityEngine;

namespace IsekaiTruck.World
{
    [Flags]
    public enum RoadConnection
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3
    }

    [DisallowMultipleComponent]
    public sealed class ModernCityChunkPrototype : MonoBehaviour
    {
        private const RoadConnection CardinalRoadConnections =
            RoadConnection.North |
            RoadConnection.East |
            RoadConnection.South |
            RoadConnection.West;

        [SerializeField] private Vector2 size = new Vector2(50f, 50f);
        [SerializeField] private RoadConnection roadConnections;

        public Vector2 Size => size;
        public RoadConnection RoadConnections => roadConnections & CardinalRoadConnections;

#if UNITY_EDITOR
        public void Configure(Vector2 chunkSize, RoadConnection connections)
        {
            size = chunkSize;
            roadConnections = connections;
        }
#endif
    }
}
