using System;
using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Wanted
{
    [DisallowMultipleComponent]
    public sealed class WantedLevelSystem : MonoBehaviour
    {
        private GameConfig.WantedSettings settings;
        private int totalKills;
        private int level;

        public int TotalKills => totalKills;
        public int Level => level;

        public event Action<WantedLevelSnapshot> StateChanged;

        public void Initialize(GameConfig gameConfig)
        {
            settings = gameConfig.Wanted;
            totalKills = 0;
            level = 0;
        }

        public void RegisterKill()
        {
            if (totalKills < int.MaxValue)
            {
                totalKills++;
            }

            int previousLevel = level;
            level = CalculateLevel(totalKills);
            if (level > previousLevel)
            {
                Debug.Log($"지명수배 레벨 상승! Lv.{level}", this);
            }

            StateChanged?.Invoke(GetState());
        }

        public void RestoreState(int savedTotalKills)
        {
            totalKills = Mathf.Max(0, savedTotalKills);
            level = CalculateLevel(totalKills);
            StateChanged?.Invoke(GetState());
        }

        public WantedLevelSnapshot GetState()
        {
            return new WantedLevelSnapshot(totalKills, level);
        }

        private int CalculateLevel(int kills)
        {
            return Mathf.Min(kills / settings.KillsPerLevel, settings.MaxLevel);
        }
    }

    public readonly struct WantedLevelSnapshot
    {
        public WantedLevelSnapshot(int totalKills, int level)
        {
            TotalKills = totalKills;
            Level = level;
        }

        public int TotalKills { get; }
        public int Level { get; }
    }
}
