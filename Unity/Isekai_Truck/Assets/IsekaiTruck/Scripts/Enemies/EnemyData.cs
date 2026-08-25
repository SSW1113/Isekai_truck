using UnityEngine;

namespace IsekaiTruck.Enemies
{
    public sealed class EnemyData
    {
        public EnemyData(string id, string displayName, float size, float collisionRadius, float moveSpeed, int contactDamage, float spawnWeight)
        {
            Id = id;
            DisplayName = displayName;
            Size = size;
            CollisionRadius = collisionRadius;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            SpawnWeight = spawnWeight;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public float Size { get; }
        public float CollisionRadius { get; }
        public float MoveSpeed { get; }
        public int ContactDamage { get; }
        public float SpawnWeight { get; }
    }
}
