using UnityEngine;

namespace IsekaiTruck.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyDefinition : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string typeId = "basic_enemy";
        [SerializeField] private string displayName = "기본 적";

        [Header("Stats")]
        [SerializeField, Min(0.01f)] private float size = 1f;
        [SerializeField, Min(0f)] private float collisionRadius = 0.5f;
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(1)] private int contactDamage = 1;
        [SerializeField, Min(0f)] private float spawnWeight = 1f;

        public string TypeId => typeId;
        public string DisplayName => displayName;
        public float Size => size;
        public float CollisionRadius => collisionRadius;
        public float MoveSpeed => moveSpeed;
        public int ContactDamage => contactDamage;
        public float SpawnWeight => spawnWeight;

        public EnemyData CreateData()
        {
            return new EnemyData(typeId, displayName, size, collisionRadius, moveSpeed, contactDamage, spawnWeight);
        }

#if UNITY_EDITOR
        public void Configure(string id, string name, float targetSize, float targetCollisionRadius, float speed, int damage, float weight)
        {
            typeId = id;
            displayName = name;
            size = targetSize;
            collisionRadius = targetCollisionRadius;
            moveSpeed = speed;
            contactDamage = damage;
            spawnWeight = weight;
        }
#endif
    }
}
