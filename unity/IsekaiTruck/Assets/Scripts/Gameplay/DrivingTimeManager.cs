using System;
using UnityEngine;

namespace IsekaiTruck.Gameplay
{
    public sealed class DrivingTimeManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float startingTime = 90f;
        [SerializeField] private float remainingTime;

        private bool isRunning;
        private bool expirationRaised;

        public event Action<float> TimeChanged;
        public event Action TimeExpired;

        public float RemainingTime => remainingTime;
        public float StartingTime => startingTime;
        public bool IsRunning => isRunning;

        private void Awake()
        {
            remainingTime = startingTime;
        }

        private void Update()
        {
            if (!isRunning || remainingTime <= 0f)
            {
                return;
            }

            SetRemainingTime(remainingTime - Time.deltaTime);

            if (remainingTime <= 0f && !expirationRaised)
            {
                expirationRaised = true;
                isRunning = false;
                TimeExpired?.Invoke();
            }
        }

        public void StartTimer()
        {
            isRunning = remainingTime > 0f;
            TimeChanged?.Invoke(remainingTime);
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        public void AddTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            expirationRaised = false;
            SetRemainingTime(remainingTime + seconds);
        }

        private void SetRemainingTime(float value)
        {
            remainingTime = Mathf.Max(0f, value);
            TimeChanged?.Invoke(remainingTime);
        }

        private void OnValidate()
        {
            startingTime = Mathf.Max(0f, startingTime);
        }
    }
}
