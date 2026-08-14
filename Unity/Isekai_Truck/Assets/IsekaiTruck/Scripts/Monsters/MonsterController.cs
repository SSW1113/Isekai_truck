using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterController : MonoBehaviour
    {
        private MonsterData type;
        private Transform truck;
        private MonsterView monsterView;
        private float referenceFrameRate;
        private float wanderAngle;
        private float nextWanderChange;
        private float fleeDirX;
        private float fleeDirZ;
        private bool hasFleeDirection;
        private float stunRemaining;

        public MonsterData Type => type;
        public bool IsStunned => stunRemaining > 0f;

        public void Initialize(MonsterData monsterType, Transform truckTransform, float nowMilliseconds, float frameRate)
        {
            type = monsterType;
            truck = truckTransform;
            monsterView = GetComponent<MonsterView>();
            referenceFrameRate = frameRate;
            monsterView?.Initialize(type.Color);
            wanderAngle = Random.value * Mathf.PI * 2f;
            nextWanderChange = nowMilliseconds + 1000f + Random.value * 2000f;
            fleeDirX = 0f;
            fleeDirZ = 0f;
            hasFleeDirection = false;
            stunRemaining = 0f;
        }

        public void UpdateMonster(float nowMilliseconds, float extraFleeDistance, float directionLockDistance, float frameScale, float deltaTime, float slowRadius, float slowMultiplier, bool isWorldPaused)
        {
            monsterView?.SetPaused(isWorldPaused);
            if (isWorldPaused)
            {
                return;
            }

            if (stunRemaining > 0f)
            {
                stunRemaining = Mathf.Max(0f, stunRemaining - deltaTime);
                monsterView?.SetMovement(Vector3.zero, 0f, false);
                return;
            }

            float dx = transform.position.x - truck.position.x;
            float dz = transform.position.z - truck.position.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            float fleeDistance = type.FleeDistance + extraFleeDistance;
            float movementMultiplier = slowRadius > 0f && distance <= slowRadius ? slowMultiplier : 1f;

            // 트럭을 인식하면 도망
            if (distance < fleeDistance && distance > 0.001f)
            {
                if (distance > directionLockDistance || !hasFleeDirection)
                {
                    fleeDirX = dx / distance;
                    fleeDirZ = dz / distance;
                    hasFleeDirection = true;
                }

                Vector3 fleeDirection = new Vector3(fleeDirX, 0f, fleeDirZ);
                float fleeSpeed = type.Speed * movementMultiplier;
                transform.position += fleeDirection * fleeSpeed * frameScale;
                monsterView?.SetMovement(fleeDirection, fleeSpeed * referenceFrameRate, true);
                return;
            }

            hasFleeDirection = false;

            // 배회 방향 변경
            if (nowMilliseconds >= nextWanderChange)
            {
                wanderAngle = Random.value * Mathf.PI * 2f;
                nextWanderChange = nowMilliseconds + 1500f + Random.value * 2000f;
            }

            float wanderSpeed = type.Speed * 0.2f * movementMultiplier;
            Vector3 wanderDirection = new Vector3(
                Mathf.Cos(wanderAngle),
                0f,
                Mathf.Sin(wanderAngle)
            );
            transform.position += wanderDirection * wanderSpeed * frameScale;
            monsterView?.SetMovement(wanderDirection, wanderSpeed * referenceFrameRate, false);
        }

        public void ApplyStun(float duration)
        {
            stunRemaining = Mathf.Max(stunRemaining, duration);
        }
    }
}
