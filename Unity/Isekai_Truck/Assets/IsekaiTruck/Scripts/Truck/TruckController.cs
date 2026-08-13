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
        private float speed;
        private float lastDirX;
        private float lastDirZ;
        private int speedLevel;
        private int sizeLevel;
        private float maxSpeed;

        public float CurrentSpeed => speed;
        public float CurrentFrameDistance { get; private set; }
        public float CurrentSpeedPerSecond { get; private set; }
        public float CurrentInputMagnitude { get; private set; }

        public void Initialize(GameConfig gameConfig)
        {
            settings = gameConfig.Truck;
            maxSpeed = settings.BaseMaxSpeed + speedLevel * settings.SpeedPerUpgrade;
        }

        public void UpdateTruck(Vector2 move)
        {
            // Three.js와 동일한 프레임 기반 이동
            Vector3 startPosition = transform.position;
            float inputLength = Mathf.Sqrt(move.x * move.x + move.y * move.y);
            CurrentInputMagnitude = inputLength;

            if (inputLength > InputThreshold)
            {
                float dirX = move.x / inputLength;
                float dirZ = move.y / inputLength;

                float currentRotation = transform.eulerAngles.y * Mathf.Deg2Rad;
                // Three.js와 Unity의 화면 좌우 축 차이 보정
                float targetRotation = Mathf.Atan2(-dirX, dirZ);
                float angleDiff = Mathf.DeltaAngle(currentRotation * Mathf.Rad2Deg, targetRotation * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                currentRotation += angleDiff * settings.TurnSpeed;

                transform.rotation = Quaternion.Euler(0f, currentRotation * Mathf.Rad2Deg, 0f);

                speed += settings.Acceleration * inputLength;
                speed = Mathf.Min(speed, maxSpeed);

                float forwardX = Mathf.Sin(currentRotation);
                float forwardZ = Mathf.Cos(currentRotation);

                lastDirX = forwardX;
                lastDirZ = forwardZ;

                transform.position += new Vector3(forwardX * speed, 0f, forwardZ * speed);
            }
            else
            {
                speed *= settings.Friction;
                transform.position += new Vector3(lastDirX * speed, 0f, lastDirZ * speed);

                if (speed < StopSpeedThreshold)
                {
                    speed = 0f;
                }
            }

            CurrentFrameDistance = Vector3.Distance(startPosition, transform.position);
            CurrentSpeedPerSecond = Time.deltaTime > 0f ? CurrentFrameDistance / Time.deltaTime : 0f;
        }

        public void UpgradeSpeed()
        {
            speedLevel++;
            maxSpeed = settings.BaseMaxSpeed + speedLevel * settings.SpeedPerUpgrade;
        }

        public void UpgradeSize()
        {
            sizeLevel++;

            float scale = 1f + sizeLevel * settings.SizePerUpgrade;
            transform.localScale = Vector3.one * scale;
            transform.position = new Vector3(transform.position.x, 0.5f * scale, transform.position.z);
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
