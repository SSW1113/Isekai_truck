using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Truck
{
    [DisallowMultipleComponent]
    public sealed class TruckController : MonoBehaviour
    {
        private const float InputThreshold = 0.05f;
        private const float StopSpeedThreshold = 0.001f;

        private GameConfig.TruckSettings settings;
        private float referenceFrameRate;
        private float speed;
        private float lastDirX;
        private float lastDirZ;
        private int speedLevel;
        private int sizeLevel;
        private float maxSpeed;
        private float blessingSpeedMultiplier = 1f;
        private float blessingSizeMultiplier = 1f;
        private float environmentSpeedMultiplier = 1f;

        public float CurrentSpeed => speed * environmentSpeedMultiplier;
        public float CurrentFrameDistance { get; private set; }
        public float CurrentSpeedPerSecond { get; private set; }
        public float CurrentMaxSpeedPerSecond => maxSpeed * referenceFrameRate;
        public float CurrentInputMagnitude { get; private set; }
        public float EnvironmentSpeedMultiplier => environmentSpeedMultiplier;

        public void Initialize(GameConfig gameConfig)
        {
            settings = gameConfig.Truck;
            referenceFrameRate = gameConfig.ReferenceFrameRate;
            environmentSpeedMultiplier = 1f;
            ApplyProgressionStats();
        }

        public void UpdateTruck(Vector2 move, float deltaTime)
        {
            Vector3 startPosition = transform.position;
            float frameScale = Mathf.Max(deltaTime, 0f) * referenceFrameRate;
            float inputLength = Mathf.Sqrt(move.x * move.x + move.y * move.y);
            CurrentInputMagnitude = inputLength;

            float remainingFrameScale = frameScale;
            while (remainingFrameScale > 0f)
            {
                float stepFrameScale = Mathf.Min(remainingFrameScale, 1f);
                UpdateStep(move, inputLength, stepFrameScale);
                remainingFrameScale -= stepFrameScale;
            }

            CurrentFrameDistance = Vector3.Distance(startPosition, transform.position);
            CurrentSpeedPerSecond = deltaTime > 0f ? CurrentFrameDistance / deltaTime : 0f;
        }

        private void UpdateStep(Vector2 move, float inputLength, float frameScale)
        {
            if (inputLength > InputThreshold)
            {
                float dirX = move.x / inputLength;
                float dirZ = move.y / inputLength;

                float currentRotation = transform.eulerAngles.y * Mathf.Deg2Rad;
                // Three.js와 Unity의 화면 좌우 축 차이 보정
                float targetRotation = Mathf.Atan2(-dirX, dirZ);
                float angleDiff = Mathf.DeltaAngle(currentRotation * Mathf.Rad2Deg, targetRotation * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                float turnFactor = GetFrameAdjustedFactor(settings.TurnSpeed, frameScale);
                currentRotation += angleDiff * turnFactor;

                transform.rotation = Quaternion.Euler(0f, currentRotation * Mathf.Rad2Deg, 0f);

                float forwardX = Mathf.Sin(currentRotation);
                float forwardZ = Mathf.Cos(currentRotation);

                lastDirX = forwardX;
                lastDirZ = forwardZ;

                float distance = GetAcceleratedDistance(settings.Acceleration * inputLength, frameScale);
                distance *= environmentSpeedMultiplier;
                transform.position += new Vector3(forwardX * distance, 0f, forwardZ * distance);
            }
            else
            {
                float distance = GetFrictionDistance(frameScale);
                distance *= environmentSpeedMultiplier;
                transform.position += new Vector3(lastDirX * distance, 0f, lastDirZ * distance);

                if (speed < StopSpeedThreshold)
                {
                    speed = 0f;
                }
            }
        }

        private float GetAcceleratedDistance(float acceleration, float frameScale)
        {
            float startSpeed = speed;
            speed = Mathf.Min(startSpeed + acceleration * frameScale, maxSpeed);

            float distance = startSpeed * frameScale + acceleration * frameScale * (frameScale + 1f) * 0.5f;
            return Mathf.Min(distance, maxSpeed * frameScale);
        }

        private float GetFrictionDistance(float frameScale)
        {
            float startSpeed = speed;

            if (settings.Friction >= 1f)
            {
                return startSpeed * frameScale;
            }

            float frictionFactor = Mathf.Pow(settings.Friction, frameScale);
            speed = startSpeed * frictionFactor;
            return settings.Friction <= 0f
                ? 0f
                : startSpeed * settings.Friction * (1f - frictionFactor) / (1f - settings.Friction);
        }

        private static float GetFrameAdjustedFactor(float perFrameFactor, float frameScale)
        {
            if (perFrameFactor <= 0f || frameScale <= 0f)
            {
                return 0f;
            }

            if (perFrameFactor >= 1f)
            {
                return 1f;
            }

            return 1f - Mathf.Pow(1f - perFrameFactor, frameScale);
        }

        public void UpgradeSpeed()
        {
            speedLevel++;
            ApplyProgressionStats();
        }

        public void UpgradeSize()
        {
            sizeLevel++;
            ApplyProgressionStats();
        }

        public void ResetUpgrades()
        {
            speedLevel = 0;
            sizeLevel = 0;
            ResetMovement();
            ApplyProgressionStats();
        }

        public void RestoreProgress(int savedSpeedLevel, int savedSizeLevel, Vector3 savedPosition, float savedYaw)
        {
            speedLevel = Mathf.Max(0, savedSpeedLevel);
            sizeLevel = Mathf.Max(0, savedSizeLevel);
            ResetMovement();
            ApplyProgressionStats();
            transform.position = new Vector3(savedPosition.x, transform.position.y, savedPosition.z);
            transform.rotation = Quaternion.Euler(0f, savedYaw, 0f);
        }

        public void Respawn(Vector3 respawnPosition, float respawnYaw)
        {
            ResetMovement();
            float scale = transform.localScale.y;
            transform.position = new Vector3(respawnPosition.x, 0.5f * scale, respawnPosition.z);
            transform.rotation = Quaternion.Euler(0f, respawnYaw, 0f);
        }

        private void ApplyProgressionStats()
        {
            maxSpeed = (settings.BaseMaxSpeed + speedLevel * settings.SpeedPerUpgrade) * blessingSpeedMultiplier;
            speed = Mathf.Min(speed, maxSpeed);
            float scale = (1f + sizeLevel * settings.SizePerUpgrade) * blessingSizeMultiplier;
            transform.localScale = Vector3.one * scale;
            transform.position = new Vector3(transform.position.x, 0.5f * scale, transform.position.z);
        }

        public void SetBlessingMultipliers(float speedMultiplier, float sizeMultiplier)
        {
            float newSpeedMultiplier = Mathf.Max(0f, speedMultiplier);
            float newSizeMultiplier = Mathf.Max(0.01f, sizeMultiplier);
            if (Mathf.Approximately(blessingSpeedMultiplier, newSpeedMultiplier) && Mathf.Approximately(blessingSizeMultiplier, newSizeMultiplier))
            {
                return;
            }

            blessingSpeedMultiplier = newSpeedMultiplier;
            blessingSizeMultiplier = newSizeMultiplier;
            ApplyProgressionStats();
        }

        public void SetEnvironmentSpeedMultiplier(float speedMultiplier)
        {
            float newSpeedMultiplier = Mathf.Max(0f, speedMultiplier);
            if (Mathf.Approximately(environmentSpeedMultiplier, newSpeedMultiplier))
            {
                return;
            }

            environmentSpeedMultiplier = newSpeedMultiplier;
        }

        private void ResetMovement()
        {
            speed = 0f;
            lastDirX = 0f;
            lastDirZ = 0f;
            CurrentFrameDistance = 0f;
            CurrentSpeedPerSecond = 0f;
            CurrentInputMagnitude = 0f;
        }

        public TruckStats GetStats()
        {
            return new TruckStats(speedLevel, sizeLevel, maxSpeed, transform.localScale.x);
        }

        public readonly struct TruckStats
        {
            public TruckStats(int speedLevel, int sizeLevel, float maxSpeed, float sizeScale)
            {
                SpeedLevel = speedLevel;
                SizeLevel = sizeLevel;
                MaxSpeed = maxSpeed;
                SizeScale = sizeScale;
            }

            public int SpeedLevel { get; }
            public int SizeLevel { get; }
            public float MaxSpeed { get; }
            public float SizeScale { get; }
        }
    }
}
