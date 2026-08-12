using IsekaiTruck.Player;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class MonsterController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform truck;
        [SerializeField] private PlayerProgress playerProgress;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int experienceReward;
        [SerializeField, Min(0)] private int soulReward;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float wanderSpeed = 1.2f;
        [SerializeField, Min(0f)] private float fleeSpeed = 4f;
        [SerializeField, Min(0.1f)] private float fleeDistance = 8f;
        [SerializeField, Min(0.1f)] private float directionLockDistance = 3f;
        [SerializeField, Min(0.1f)] private float minWanderDirectionTime = 1.5f;
        [SerializeField, Min(0.1f)] private float maxWanderDirectionTime = 3.5f;

        private Rigidbody monsterRigidbody;
        private Vector3 wanderDirection;
        private Vector3 fleeDirection;
        private float nextWanderDirectionTime;
        private bool isFleeing;
        private bool isDefeated;

        public void Configure(Transform truckTarget, PlayerProgress progress)
        {
            truck = truckTarget;
            playerProgress = progress;
        }

        public void ConfigureRewards(int experience, int soul)
        {
            experienceReward = Mathf.Max(0, experience);
            soulReward = Mathf.Max(0, soul);
        }

        private void Awake()
        {
            monsterRigidbody = GetComponent<Rigidbody>();
            ChooseNewWanderDirection();
        }

        private void FixedUpdate()
        {
            if (isDefeated)
            {
                return;
            }

            Vector3 moveDirection;
            float moveSpeed;

            if (TryGetFleeDirection(out Vector3 currentFleeDirection))
            {
                moveDirection = currentFleeDirection;
                moveSpeed = fleeSpeed;
            }
            else
            {
                if (Time.time >= nextWanderDirectionTime)
                {
                    ChooseNewWanderDirection();
                }

                moveDirection = wanderDirection;
                moveSpeed = wanderSpeed;
            }

            Vector3 nextPosition = monsterRigidbody.position
                + moveDirection * (moveSpeed * Time.fixedDeltaTime);
            nextPosition.y = monsterRigidbody.position.y;
            monsterRigidbody.MovePosition(nextPosition);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDefeated || truck == null)
            {
                return;
            }

            Rigidbody otherBody = other.attachedRigidbody;
            bool touchedTruck = other.transform == truck
                || other.transform.IsChildOf(truck)
                || (otherBody != null && otherBody.transform == truck);

            if (!touchedTruck)
            {
                return;
            }

            isDefeated = true;
            playerProgress?.RegisterMonsterDefeat(experienceReward, soulReward);
            Destroy(gameObject);
        }

        private bool TryGetFleeDirection(out Vector3 direction)
        {
            direction = Vector3.zero;
            if (truck == null)
            {
                isFleeing = false;
                return false;
            }

            Vector3 awayFromTruck = transform.position - truck.position;
            awayFromTruck.y = 0f;
            float sqrDistance = awayFromTruck.sqrMagnitude;

            if (sqrDistance >= fleeDistance * fleeDistance)
            {
                isFleeing = false;
                return false;
            }

            if (!isFleeing || sqrDistance > directionLockDistance * directionLockDistance)
            {
                fleeDirection = sqrDistance > 0.0001f
                    ? awayFromTruck.normalized
                    : RandomPlanarDirection();
            }

            isFleeing = true;
            direction = fleeDirection;
            return true;
        }

        private void ChooseNewWanderDirection()
        {
            wanderDirection = RandomPlanarDirection();

            float minimum = Mathf.Min(minWanderDirectionTime, maxWanderDirectionTime);
            float maximum = Mathf.Max(minWanderDirectionTime, maxWanderDirectionTime);
            nextWanderDirectionTime = Time.time + Random.Range(minimum, maximum);
        }

        private static Vector3 RandomPlanarDirection()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
