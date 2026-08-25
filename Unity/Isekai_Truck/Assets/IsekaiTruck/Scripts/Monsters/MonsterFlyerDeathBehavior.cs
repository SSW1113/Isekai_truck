using IsekaiTruck.UI;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterFlyerDeathBehavior : MonsterContactBehavior
    {
        [Header("Flyer Screen Effect")]
        [SerializeField] private FlyerScreenOverlay screenOverlayPrefab;

        private bool hasTriggered;

        public FlyerScreenOverlay ScreenOverlayPrefab => screenOverlayPrefab;

        protected override void OnInitialized()
        {
            hasTriggered = false;
        }

        protected override bool TryResolveContact(
            MonsterContactContext context,
            out MonsterContactResult result)
        {
            if (!hasTriggered && screenOverlayPrefab != null)
            {
                hasTriggered = true;
                Instantiate(screenOverlayPrefab);
            }

            result = default;
            return false;
        }
    }
}
