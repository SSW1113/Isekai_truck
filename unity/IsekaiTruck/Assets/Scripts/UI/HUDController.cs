using IsekaiTruck.Gameplay;
using IsekaiTruck.Player;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private DrivingTimeManager drivingTimeManager;
        [SerializeField] private TruckController truckController;

        [Header("Text")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text experienceText;
        [SerializeField] private Text drivingTimeText;
        [SerializeField] private Text soulText;
        [SerializeField] private Text speedText;

        [Header("Gauges")]
        [SerializeField] private Image experienceFill;
        [SerializeField] private ArcGraphic drivingTimeArc;
        [SerializeField] private ArcGraphic speedArc;
        [SerializeField] private RectTransform speedNeedle;
        [SerializeField, Min(1f)] private float maximumDisplaySpeed = 30f;
        [SerializeField, Min(0.01f)] private float speedDisplayMultiplier = 3.6f;

        public void Configure(
            PlayerProgress progress,
            DrivingTimeManager timer,
            TruckController truck,
            Text level,
            Text experience,
            Text drivingTime,
            Text soul,
            Text speed,
            Image expFill,
            ArcGraphic timeArc,
            ArcGraphic arc,
            RectTransform needle)
        {
            playerProgress = progress;
            drivingTimeManager = timer;
            truckController = truck;
            levelText = level;
            experienceText = experience;
            drivingTimeText = drivingTime;
            soulText = soul;
            speedText = speed;
            experienceFill = expFill;
            drivingTimeArc = timeArc;
            speedArc = arc;
            speedNeedle = needle;
        }

        private void OnEnable()
        {
            if (playerProgress != null)
            {
                playerProgress.ProgressChanged += RefreshProgress;
            }

            if (drivingTimeManager != null)
            {
                drivingTimeManager.TimeChanged += RefreshDrivingTime;
            }

            RefreshProgress();
            RefreshDrivingTime(drivingTimeManager != null ? drivingTimeManager.RemainingTime : 0f);
            RefreshSpeed();
        }

        private void OnDisable()
        {
            if (playerProgress != null)
            {
                playerProgress.ProgressChanged -= RefreshProgress;
            }

            if (drivingTimeManager != null)
            {
                drivingTimeManager.TimeChanged -= RefreshDrivingTime;
            }
        }

        private void Update()
        {
            RefreshSpeed();
        }

        private void RefreshProgress()
        {
            if (playerProgress == null)
            {
                return;
            }

            if (levelText != null)
            {
                levelText.text = $"LV. {playerProgress.Level}";
            }

            if (experienceText != null)
            {
                experienceText.text = $"{playerProgress.CurrentExperience} / {playerProgress.RequiredExperience}";
            }

            if (experienceFill != null)
            {
                experienceFill.fillAmount = playerProgress.ExperienceNormalized;
            }

            if (soulText != null)
            {
                soulText.text = playerProgress.CurrentSoul.ToString();
            }
        }

        private void RefreshDrivingTime(float seconds)
        {
            if (drivingTimeText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            drivingTimeText.text = $"{minutes:00}:{remainingSeconds:00}";

            if (drivingTimeArc != null && drivingTimeManager != null)
            {
                float startingTime = Mathf.Max(0.01f, drivingTimeManager.StartingTime);
                drivingTimeArc.FillAmount = Mathf.Clamp01(seconds / startingTime);
            }
        }

        private void RefreshSpeed()
        {
            float displaySpeed = truckController != null
                ? truckController.CurrentSpeed * speedDisplayMultiplier
                : 0f;
            float normalized = Mathf.Clamp01(displaySpeed / maximumDisplaySpeed);

            if (speedText != null)
            {
                speedText.text = Mathf.RoundToInt(displaySpeed).ToString();
            }

            if (speedArc != null)
            {
                speedArc.FillAmount = normalized;
            }

            if (speedNeedle != null)
            {
                speedNeedle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(105f, -105f, normalized));
            }
        }
    }
}
