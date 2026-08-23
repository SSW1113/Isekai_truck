using System;
using UnityEngine;

namespace IsekaiTruck.UI.Entry
{
    [DisallowMultipleComponent]
    public sealed class TitleTruckEntrance : MonoBehaviour
    {
        [SerializeField] private Transform truck;
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField, Min(0.1f)] private float animationDuration = 1.8f;
        [SerializeField] private AnimationCurve animationCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 1.2f, 1.2f),
            new Keyframe(0.74f, 0.89f, 1.2f, 1.2f),
            new Keyframe(1f, 1f, 0f, 0f)
        );
        [SerializeField] private Vector3 pathCurveOffset = new Vector3(-5f, 4f, 0f);
        [SerializeField] private Vector3 startRotation = new Vector3(0f, -8f, -2f);
        [SerializeField] private Vector3 endRotation = new Vector3(0f, 4f, 0f);
        [SerializeField, Range(0.5f, 0.95f)] private float brakingStartTime = 0.74f;
        [SerializeField, Range(0f, 15f)] private float brakePitchAngle = 8f;
        [SerializeField, Range(0f, 0.5f)] private float suspensionStrength = 0.12f;

        private const float BrakeDiveDuration = 0.28f;
        private const float BrakeReboundCycles = 1.25f;

        private float elapsedTime;

        public bool IsComplete { get; private set; }

        public event Action Completed;

        private void Awake()
        {
            ResetEntrance();
        }

        private void Update()
        {
            if (elapsedTime >= animationDuration)
            {
                return;
            }

            elapsedTime = Mathf.Min(elapsedTime + Time.unscaledDeltaTime, animationDuration);
            float normalizedTime = elapsedTime / animationDuration;
            float movementProgress = Mathf.Clamp01(animationCurve.Evaluate(normalizedTime));

            Vector3 position = EvaluatePosition(movementProgress);
            Quaternion rotation = EvaluateRotation(movementProgress);
            float brakeResponse = EvaluateBrakeResponse(normalizedTime);

            truck.position = position + Vector3.down * (brakeResponse * suspensionStrength);
            truck.rotation = rotation * Quaternion.Euler(brakeResponse * brakePitchAngle, 0f, 0f);

            if (elapsedTime >= animationDuration)
            {
                IsComplete = true;
                Completed?.Invoke();
            }
        }

        private void ResetEntrance()
        {
            elapsedTime = 0f;
            IsComplete = false;
            truck.position = startPoint.position;
            truck.rotation = EvaluateRotation(0f);
        }

        private Vector3 EvaluatePosition(float progress)
        {
            Vector3 linearPosition = Vector3.LerpUnclamped(startPoint.position, endPoint.position, progress);
            float curveWeight = 4f * progress * (1f - progress);
            return linearPosition + pathCurveOffset * curveWeight;
        }

        private Quaternion EvaluateRotation(float progress)
        {
            Vector3 pathDirection = endPoint.position - startPoint.position + pathCurveOffset * (4f - 8f * progress);
            Quaternion directionRotation = pathDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(pathDirection.normalized, Vector3.up)
                : endPoint.rotation;
            Quaternion rotationOffset = Quaternion.SlerpUnclamped(
                Quaternion.Euler(startRotation),
                Quaternion.Euler(endRotation),
                progress
            );
            return directionRotation * rotationOffset;
        }

        private float EvaluateBrakeResponse(float normalizedTime)
        {
            if (normalizedTime <= brakingStartTime)
            {
                return 0f;
            }

            float brakingProgress = Mathf.InverseLerp(brakingStartTime, 1f, normalizedTime);
            if (brakingProgress <= BrakeDiveDuration)
            {
                return Mathf.SmoothStep(0f, 1f, brakingProgress / BrakeDiveDuration);
            }

            float reboundProgress = Mathf.InverseLerp(BrakeDiveDuration, 1f, brakingProgress);
            float damping = 1f - reboundProgress;
            return Mathf.Cos(reboundProgress * Mathf.PI * 2f * BrakeReboundCycles) * damping * damping;
        }

        private void OnDrawGizmosSelected()
        {
            if (startPoint == null || endPoint == null)
            {
                return;
            }

            Gizmos.color = new Color(0.15f, 0.65f, 1f, 1f);
            Vector3 previousPosition = startPoint.position;
            for (int i = 1; i <= 24; i++)
            {
                float progress = i / 24f;
                Vector3 position = EvaluatePosition(progress);
                Gizmos.DrawLine(previousPosition, position);
                previousPosition = position;
            }

            Gizmos.DrawWireSphere(startPoint.position, 0.3f);
            Gizmos.DrawWireSphere(endPoint.position, 0.3f);
        }

#if UNITY_EDITOR
        public void SetReferences(Transform targetTruck, Transform targetStartPoint, Transform targetEndPoint)
        {
            truck = targetTruck;
            startPoint = targetStartPoint;
            endPoint = targetEndPoint;
        }
#endif
    }
}
