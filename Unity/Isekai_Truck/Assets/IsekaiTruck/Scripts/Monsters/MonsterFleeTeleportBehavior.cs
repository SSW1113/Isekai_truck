using IsekaiTruck.Audio;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterFleeTeleportBehavior : MonsterMovementBehavior
    {
        [Header("Flee Teleport")]
        [SerializeField, Min(0.1f)] private float teleportInterval = 3f;
        [SerializeField, Min(0f)] private float teleportDistanceMultiplier = 1f;

        private float teleportCooldownElapsed;

        public float TeleportInterval => teleportInterval;
        public float TeleportDistanceMultiplier => teleportDistanceMultiplier;

        protected override void OnInitialized()
        {
            teleportCooldownElapsed = 0f;
        }

        protected override bool TryUpdateMovement(MonsterMovementContext context)
        {
            float interval = Mathf.Max(0.1f, teleportInterval);
            teleportCooldownElapsed = Mathf.Min(
                interval,
                teleportCooldownElapsed + Mathf.Max(0f, context.DeltaTime)
            );

            Vector3 awayFromTruck = transform.position - Truck.position;
            awayFromTruck.y = 0f;

            float distance = awayFromTruck.magnitude;
            float fleeDistance = Type.FleeDistance + context.ExtraFleeDistance;
            if (distance >= fleeDistance || distance <= 0.001f)
            {
                return false;
            }

            if (teleportCooldownElapsed < interval)
            {
                return false;
            }

            teleportCooldownElapsed = 0f;
            bool playTeleportSound = IsVisibleToGameCamera();
            float teleportDistance = Type.FleeDistance * teleportDistanceMultiplier;
            transform.position += awayFromTruck / distance * teleportDistance;
            if (playTeleportSound)
            {
                GameSfxPlayer.PlayWizardTeleport();
            }

            return false;
        }
    }
}
