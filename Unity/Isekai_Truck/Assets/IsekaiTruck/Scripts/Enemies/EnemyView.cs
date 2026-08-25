using IsekaiTruck.Visuals;
using UnityEngine;

namespace IsekaiTruck.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private DirectionalSpriteAnimator directionalSpriteAnimator;

        private void Awake()
        {
            ResolveReferences();
            directionalSpriteAnimator?.Initialize();
        }

        public void SetMovement(Vector3 direction, float moveSpeed)
        {
            directionalSpriteAnimator?.SetMovement(direction, moveSpeed);
        }

        public void SetPaused(bool isPaused)
        {
            directionalSpriteAnimator?.SetPaused(isPaused);
        }

        private void ResolveReferences()
        {
            if (directionalSpriteAnimator == null)
            {
                directionalSpriteAnimator = GetComponentInChildren<DirectionalSpriteAnimator>(true);
            }
        }

#if UNITY_EDITOR
        public void Configure(DirectionalSpriteAnimator spriteAnimator)
        {
            directionalSpriteAnimator = spriteAnimator;
        }
#endif
    }
}
