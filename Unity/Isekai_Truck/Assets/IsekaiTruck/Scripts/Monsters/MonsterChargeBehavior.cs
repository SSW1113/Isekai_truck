using IsekaiTruck.Audio;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterChargeBehavior : MonsterMovementBehavior
    {
        [Header("Charge")]
        [SerializeField, Min(0f)] private float recognitionDistanceMultiplier = 1f;
        [SerializeField, Min(0f)] private float chargeSpeedMultiplier = 1f;

        private bool isCharging;

        public float RecognitionDistanceMultiplier => recognitionDistanceMultiplier;
        public float ChargeSpeedMultiplier => chargeSpeedMultiplier;

        protected override void OnInitialized()
        {
            isCharging = false;
        }

        protected override bool TryUpdateMovement(MonsterMovementContext context)
        {
            Vector3 toTruck = Truck.position - transform.position;
            toTruck.y = 0f;

            float distance = toTruck.magnitude;
            float recognitionDistance = Type.FleeDistance * recognitionDistanceMultiplier + context.ExtraFleeDistance;
            if (distance >= recognitionDistance)
            {
                isCharging = false;
                return false;
            }

            if (!isCharging)
            {
                isCharging = true;
                if (IsVisibleToGameCamera())
                {
                    GameSfxPlayer.PlayRandomSamuraiCharge();
                }
            }

            if (distance <= 0.001f)
            {
                SetMovementVisual(Vector3.zero, 0f, false);
                return true;
            }

            float movementMultiplier = context.SlowRadius > 0f && distance <= context.SlowRadius
                ? context.SlowMultiplier
                : 1f;
            float chargeSpeed = Type.Speed * chargeSpeedMultiplier * movementMultiplier;
            Move(toTruck / distance, chargeSpeed, context.FrameScale, true);
            return true;
        }
    }
}
