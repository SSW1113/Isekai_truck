using System;
using IsekaiTruck.Audio;
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
        public int MaxLevel => settings.MaxLevel;

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
                GameSfxPlayer.PlayWantedLevelUp(level);
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

        public void ResetForWorldTravel()
        {
            totalKills = 0;
            level = 0;
            StateChanged?.Invoke(GetState());
        }

        public WantedLevelSnapshot GetState()
        {
            return new WantedLevelSnapshot(totalKills, level);
        }

        public int GetRequiredTotalKillsForLevel(int targetLevel)
        {
            int safeLevel = Mathf.Clamp(targetLevel, 0, settings.MaxLevel);
            long requiredKills = (long)settings.KillsPerLevel * safeLevel * (safeLevel + 1) / 2;
            return requiredKills >= int.MaxValue ? int.MaxValue : (int)requiredKills;
        }

        private int CalculateLevel(int kills)
        {
            for (int targetLevel = 1; targetLevel <= settings.MaxLevel; targetLevel++)
            {
                if (kills < GetRequiredTotalKillsForLevel(targetLevel))
                {
                    return targetLevel - 1;
                }
            }

            return settings.MaxLevel;
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
