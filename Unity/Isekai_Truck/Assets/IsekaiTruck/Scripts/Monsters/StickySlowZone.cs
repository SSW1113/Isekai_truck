using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class StickySlowZone : MonoBehaviour
    {
        [Header("Sticky Slow")]
        [SerializeField, Min(0f)] private float radius = 2f;
        [SerializeField, Range(0f, 1f)] private float speedMultiplier = 0.5f;

        private TruckStickySlowController slowController;

        public float Radius => radius;
        public float SpeedMultiplier => speedMultiplier;

        public void Initialize(TruckStickySlowController targetSlowController)
        {
            if (slowController == targetSlowController)
            {
                return;
            }

            slowController?.UnregisterZone(this);
            slowController = targetSlowController;
            slowController?.RegisterZone(this);
        }

        private void OnDestroy()
        {
            slowController?.UnregisterZone(this);
        }
    }
}
