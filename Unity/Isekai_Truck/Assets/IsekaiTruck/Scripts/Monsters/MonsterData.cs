using UnityEngine;

namespace IsekaiTruck.Monsters
{
    public sealed class MonsterData
    {
        public MonsterData(
            string id,
            string name,
            string colorHex,
            Color color,
            float size,
            float speed,
            float fleeDistance,
            int exp,
            int soul,
            float spawnWeight
        )
        {
            Id = id;
            Name = name;
            ColorHex = colorHex;
            Color = color;
            Size = size;
            Speed = speed;
            FleeDistance = fleeDistance;
            Exp = exp;
            Soul = soul;
            SpawnWeight = spawnWeight;
        }

        public string Id { get; }
        public string Name { get; }
        public string ColorHex { get; }
        public Color Color { get; }
        public float Size { get; }
        public float Speed { get; }
        public float FleeDistance { get; }
        public int Exp { get; }
        public int Soul { get; }
        public float SpawnWeight { get; }
    }
}
