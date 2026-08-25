using UnityEngine;

namespace IsekaiTruck.Monsters
{
    public readonly struct MonsterMovementContext
    {
        public MonsterMovementContext(
            float nowMilliseconds,
            float extraFleeDistance,
            float directionLockDistance,
            float frameScale,
            float deltaTime,
            float slowRadius,
            float slowMultiplier)
        {
            NowMilliseconds = nowMilliseconds;
            ExtraFleeDistance = extraFleeDistance;
            DirectionLockDistance = directionLockDistance;
            FrameScale = frameScale;
            DeltaTime = deltaTime;
            SlowRadius = slowRadius;
            SlowMultiplier = slowMultiplier;
        }

        public float NowMilliseconds { get; }
        public float ExtraFleeDistance { get; }
        public float DirectionLockDistance { get; }
        public float FrameScale { get; }
        public float DeltaTime { get; }
        public float SlowRadius { get; }
        public float SlowMultiplier { get; }
    }

    public abstract class MonsterMovementBehavior : MonoBehaviour
    {
        protected MonsterController Monster { get; private set; }
        protected MonsterData Type => Monster.Type;
        protected Transform Truck => Monster.Truck;

        internal void InitializeBehavior(MonsterController monster)
        {
            Monster = monster;
            OnInitialized();
        }

        internal bool TryUpdateMovementInternal(MonsterMovementContext context)
        {
            return isActiveAndEnabled && TryUpdateMovement(context);
        }

        protected void Move(Vector3 direction, float speed, float frameScale, bool isFleeing)
        {
            Monster.ApplyMovement(direction, speed, frameScale, isFleeing);
        }

        protected void SetMovementVisual(Vector3 direction, float speed, bool isFleeing)
        {
            Monster.SetMovementVisual(direction, speed, isFleeing);
        }

        protected virtual void OnInitialized()
        {
        }

        protected abstract bool TryUpdateMovement(MonsterMovementContext context);
    }
}
