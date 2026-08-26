using UnityEngine;

namespace IsekaiTruck.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyDefinition))]
    public sealed class EnemyController : MonoBehaviour
    {
        private EnemyData type;
        private Transform truck;
        private EnemyView enemyView;

        public EnemyData Type => type;

        public void Initialize(EnemyData enemyType, Transform truckTransform)
        {
            type = enemyType;
            truck = truckTransform;
            enemyView = GetComponent<EnemyView>();
        }

        public void UpdateEnemy(float deltaTime, bool isWorldPaused)
        {
            UpdateEnemy(deltaTime, isWorldPaused, 1f);
        }

        public void UpdateEnemy(float deltaTime, bool isWorldPaused, float speedMultiplier)
        {
            enemyView?.SetPaused(isWorldPaused);
            if (isWorldPaused || deltaTime <= 0f)
            {
                return;
            }

            Vector3 direction = truck.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return;
            }

            direction /= distance;
            float moveSpeed = type.MoveSpeed * Mathf.Max(0f, speedMultiplier);
            float moveDistance = Mathf.Min(moveSpeed * deltaTime, distance);
            transform.position += direction * moveDistance;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            enemyView?.SetMovement(direction, moveSpeed);
        }
    }
}
