using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    public enum MonsterContactOutcome
    {
        Defeated,
        Blocked,
        Survived,
        Ignored
    }

    public readonly struct MonsterContactResult
    {
        public MonsterContactResult(MonsterContactOutcome outcome)
        {
            Outcome = outcome;
        }

        public MonsterContactOutcome Outcome { get; }

        public static MonsterContactResult Defeated => new MonsterContactResult(MonsterContactOutcome.Defeated);
    }

    public readonly struct MonsterContactContext
    {
        public MonsterContactContext(
            Transform truck,
            TruckController truckController,
            float truckScale,
            float collisionDistance,
            Vector3 contactNormal,
            Vector3 truckForward)
        {
            Truck = truck;
            TruckController = truckController;
            TruckScale = truckScale;
            CollisionDistance = collisionDistance;
            ContactNormal = contactNormal;
            TruckForward = truckForward;
        }

        public Transform Truck { get; }
        public TruckController TruckController { get; }
        public float TruckScale { get; }
        public float CollisionDistance { get; }
        public Vector3 ContactNormal { get; }
        public Vector3 TruckForward { get; }
    }

    public abstract class MonsterContactBehavior : MonoBehaviour
    {
        protected MonsterController Monster { get; private set; }
        protected MonsterData Type => Monster.Type;

        internal void InitializeBehavior(MonsterController monster)
        {
            Monster = monster;
            OnInitialized();
        }

        internal bool TryResolveContactInternal(MonsterContactContext context, out MonsterContactResult result)
        {
            if (!isActiveAndEnabled)
            {
                result = default;
                return false;
            }

            return TryResolveContact(context, out result);
        }

        protected virtual void OnInitialized()
        {
        }

        protected abstract bool TryResolveContact(MonsterContactContext context, out MonsterContactResult result);
    }
}
