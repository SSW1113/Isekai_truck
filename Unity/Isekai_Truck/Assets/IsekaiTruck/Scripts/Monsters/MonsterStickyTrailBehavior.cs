using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterStickyTrailBehavior : MonsterMovementBehavior
    {
        [Header("Sticky Trail")]
        [SerializeField] private StickySlowZone stickyZonePrefab;
        [SerializeField, Min(0.1f)] private float dropInterval = 3f;

        private TruckStickySlowController truckSlowController;
        private float dropElapsed;

        public StickySlowZone StickyZonePrefab => stickyZonePrefab;
        public float DropInterval => dropInterval;

        protected override void OnInitialized()
        {
            dropElapsed = 0f;
            TruckController truckController = Truck.GetComponent<TruckController>();
            if (truckController == null)
            {
                truckSlowController = null;
                return;
            }

            truckSlowController = Truck.GetComponent<TruckStickySlowController>();
            if (truckSlowController == null)
            {
                truckSlowController = Truck.gameObject.AddComponent<TruckStickySlowController>();
            }
        }

        protected override bool TryUpdateMovement(MonsterMovementContext context)
        {
            if (stickyZonePrefab == null || truckSlowController == null)
            {
                return false;
            }

            float interval = Mathf.Max(0.1f, dropInterval);
            dropElapsed += Mathf.Max(0f, context.DeltaTime);
            if (dropElapsed < interval)
            {
                return false;
            }

            dropElapsed %= interval;
            Vector3 spawnPosition = transform.position;
            spawnPosition.y = 0.02f;
            StickySlowZone stickyZone = Instantiate(
                stickyZonePrefab,
                spawnPosition,
                Quaternion.identity);
            stickyZone.Initialize(truckSlowController);
            return false;
        }
    }
}
