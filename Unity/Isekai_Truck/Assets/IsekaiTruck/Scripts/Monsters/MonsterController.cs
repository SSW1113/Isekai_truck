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
        private Renderer[] visibilityRenderers = System.Array.Empty<Renderer>();
        private MonsterMovementBehavior[] movementBehaviors = System.Array.Empty<MonsterMovementBehavior>();
        private MonsterContactBehavior[] contactBehaviors = System.Array.Empty<MonsterContactBehavior>();

        public MonsterData Type => type;
        internal Transform Truck => truck;
        public bool IsStunned => stunRemaining > 0f;

        public void Initialize(MonsterData monsterType, Transform truckTransform, float nowMilliseconds, float frameRate)
        {
            type = monsterType;
            truck = truckTransform;
            monsterView = GetComponent<MonsterView>();
            referenceFrameRate = frameRate;
            monsterView?.Initialize(type.Color);
            visibilityRenderers = GetComponentsInChildren<Renderer>(true);
            wanderAngle = Random.value * Mathf.PI * 2f;
            nextWanderChange = nowMilliseconds + 1000f + Random.value * 2000f;
            fleeDirX = 0f;
            fleeDirZ = 0f;
            hasFleeDirection = false;
            stunRemaining = 0f;

            movementBehaviors = GetComponents<MonsterMovementBehavior>();
            for (int i = 0; i < movementBehaviors.Length; i++)
            {
                movementBehaviors[i].InitializeBehavior(this);
            }

            contactBehaviors = GetComponents<MonsterContactBehavior>();
            for (int i = 0; i < contactBehaviors.Length; i++)
            {
                contactBehaviors[i].InitializeBehavior(this);
            }
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

            MonsterMovementContext movementContext = new MonsterMovementContext(
                nowMilliseconds,
                extraFleeDistance,
                directionLockDistance,
                frameScale,
                deltaTime,
                slowRadius,
                slowMultiplier
            );
            for (int i = 0; i < movementBehaviors.Length; i++)
            {
                if (movementBehaviors[i].TryUpdateMovementInternal(movementContext))
                {
                    return;
                }
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
                ApplyMovement(fleeDirection, fleeSpeed, frameScale, true);
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
            ApplyMovement(wanderDirection, wanderSpeed, frameScale, false);
        }

        public MonsterContactResult ResolveContact(MonsterContactContext context)
        {
            for (int i = 0; i < contactBehaviors.Length; i++)
            {
                if (contactBehaviors[i].TryResolveContactInternal(context, out MonsterContactResult result))
                {
                    return result;
                }
            }

            return MonsterContactResult.Defeated;
        }

        internal void ApplyMovement(Vector3 direction, float moveSpeed, float frameScale, bool isFleeing)
        {
            transform.position += direction * moveSpeed * frameScale;
            SetMovementVisual(direction, moveSpeed, isFleeing);
        }

        internal void SetMovementVisual(Vector3 direction, float moveSpeed, bool isFleeing)
        {
            monsterView?.SetMovement(direction, moveSpeed * referenceFrameRate, isFleeing);
        }

        internal bool IsVisibleToGameCamera()
        {
            UnityEngine.Camera gameCamera = UnityEngine.Camera.main;
            if (gameCamera == null || !gameCamera.isActiveAndEnabled)
            {
                return false;
            }

            Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(gameCamera);
            for (int i = 0; i < visibilityRenderers.Length; i++)
            {
                Renderer targetRenderer = visibilityRenderers[i];
                if (targetRenderer != null && targetRenderer.enabled &&
                    targetRenderer.gameObject.activeInHierarchy &&
                    GeometryUtility.TestPlanesAABB(cameraPlanes, targetRenderer.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        public void ApplyStun(float duration)
        {
            stunRemaining = Mathf.Max(stunRemaining, duration);
        }

        public void BeginDefeat(Vector3 direction)
        {
            float duration = monsterView != null ? monsterView.PlayDefeat(direction) : 0f;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject, Mathf.Max(0f, duration) + 0.02f);
        }
    }
}
