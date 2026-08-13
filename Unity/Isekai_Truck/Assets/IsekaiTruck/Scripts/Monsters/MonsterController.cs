using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterController : MonoBehaviour
    {
        private MonsterData type;
        private Transform truck;
        private float wanderAngle;
        private float nextWanderChange;
        private float fleeDirX;
        private float fleeDirZ;
        private bool hasFleeDirection;

        public MonsterData Type => type;

        public void Initialize(MonsterData monsterType, Transform truckTransform, float nowMilliseconds)
        {
            type = monsterType;
            truck = truckTransform;
            wanderAngle = Random.value * Mathf.PI * 2f;
            nextWanderChange = nowMilliseconds + 1000f + Random.value * 2000f;
            fleeDirX = 0f;
            fleeDirZ = 0f;
            hasFleeDirection = false;
        }

        public void UpdateMonster(float nowMilliseconds, float extraFleeDistance, float directionLockDistance)
        {
            float dx = transform.position.x - truck.position.x;
            float dz = transform.position.z - truck.position.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            float fleeDistance = type.FleeDistance + extraFleeDistance;

            // 트럭을 인식하면 도망
            if (distance < fleeDistance && distance > 0.001f)
            {
                if (distance > directionLockDistance || !hasFleeDirection)
                {
                    fleeDirX = dx / distance;
                    fleeDirZ = dz / distance;
                    hasFleeDirection = true;
                }

                transform.position += new Vector3(fleeDirX * type.Speed, 0f, fleeDirZ * type.Speed);
                return;
            }

            hasFleeDirection = false;

            // 배회 방향 변경
            if (nowMilliseconds >= nextWanderChange)
            {
                wanderAngle = Random.value * Mathf.PI * 2f;
                nextWanderChange = nowMilliseconds + 1500f + Random.value * 2000f;
            }

            float wanderSpeed = type.Speed * 0.2f;
            transform.position += new Vector3(Mathf.Cos(wanderAngle) * wanderSpeed, 0f, Mathf.Sin(wanderAngle) * wanderSpeed);
        }
    }
}
