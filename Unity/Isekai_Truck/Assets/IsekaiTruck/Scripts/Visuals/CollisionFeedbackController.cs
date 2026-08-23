using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Visuals
{
    [DisallowMultipleComponent]
    public sealed class CollisionFeedbackController : MonoBehaviour
    {
        [Header("Hit Stop")]
        [SerializeField, Range(0f, 0.1f)] private float hitStopDuration = 0.045f;

        [Header("Impact Burst")]
        [SerializeField] private ParticleSystem impactEffectPrefab;
        [SerializeField] private Transform effectRoot;
        [SerializeField, Range(1, 32)] private int effectPoolSize = 12;
        [SerializeField, Min(0f)] private float effectHeight = 0.5f;

        [Header("Truck")]
        [SerializeField] private TruckSpriteView truckSpriteView;

        private ParticleSystem[] impactEffects;
        private int nextEffectIndex;
        private float hitStopRemaining;
        private float previousTimeScale = 1f;
        private bool isHitStopping;

        public void Initialize()
        {
            BuildEffectPool();
            hitStopRemaining = 0f;
            isHitStopping = false;
        }

        private void Update()
        {
            if (!isHitStopping)
            {
                return;
            }

            hitStopRemaining = Mathf.Max(0f, hitStopRemaining - Time.unscaledDeltaTime);
            if (hitStopRemaining <= 0f)
            {
                RestoreTimeScale();
            }
        }

        public void PlayMonsterDefeat(Vector3 worldPosition)
        {
            if (impactEffects == null || impactEffects.Length == 0)
            {
                return;
            }

            ParticleSystem effect = impactEffects[nextEffectIndex];
            nextEffectIndex = (nextEffectIndex + 1) % impactEffects.Length;
            effect.transform.position = worldPosition + Vector3.up * effectHeight;
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);
        }

        public void PlayMonsterCollisionBatch(int collisionCount)
        {
            if (collisionCount <= 0)
            {
                return;
            }

            truckSpriteView?.PlayImpactFeedback();
            RequestHitStop();
        }

        private void RequestHitStop()
        {
            if (hitStopDuration <= 0f)
            {
                return;
            }

            if (isHitStopping)
            {
                hitStopRemaining = Mathf.Max(hitStopRemaining, hitStopDuration);
                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            hitStopRemaining = hitStopDuration;
            isHitStopping = true;
            Time.timeScale = 0f;
        }

        private void BuildEffectPool()
        {
            if (impactEffectPrefab == null || impactEffects != null && impactEffects.Length == effectPoolSize)
            {
                return;
            }

            Transform parent = effectRoot != null ? effectRoot : transform;
            impactEffects = new ParticleSystem[effectPoolSize];
            for (int i = 0; i < impactEffects.Length; i++)
            {
                ParticleSystem effect = Instantiate(impactEffectPrefab, parent, false);
                effect.name = $"Impact Effect {i + 1}";
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                impactEffects[i] = effect;
            }

            nextEffectIndex = 0;
        }

        private void RestoreTimeScale()
        {
            if (!isHitStopping)
            {
                return;
            }

            isHitStopping = false;
            hitStopRemaining = 0f;
            if (Mathf.Approximately(Time.timeScale, 0f))
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private void OnDisable()
        {
            RestoreTimeScale();
        }

#if UNITY_EDITOR
        public void SetReferences(TruckSpriteView truckView, ParticleSystem effectPrefab, Transform effectsParent)
        {
            truckSpriteView = truckView;
            impactEffectPrefab = effectPrefab;
            effectRoot = effectsParent;
        }
#endif
    }
}
