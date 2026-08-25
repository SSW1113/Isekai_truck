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
            float moveDistance = Mathf.Min(type.MoveSpeed * deltaTime, distance);
            transform.position += direction * moveDistance;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            enemyView?.SetMovement(direction, type.MoveSpeed);
        }
    }
}
