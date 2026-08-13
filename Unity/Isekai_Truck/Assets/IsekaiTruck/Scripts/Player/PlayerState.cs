using System;
using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerState : MonoBehaviour
    {
        private GameConfig.PlayerSettings settings;
        private int level;
        private int exp;
        private int soul;
        private int upgradePoints;

        public int Level => level;
        public int Exp => exp;
        public int Soul => soul;
        public int UpgradePoints => upgradePoints;
        public int RequiredExp => GetRequiredExp();

        public void Initialize(GameConfig gameConfig)
        {
            settings = gameConfig.Player;
            level = settings.StartLevel;
            exp = settings.StartExp;
            soul = settings.StartSoul;
            upgradePoints = 0;
        }

        public RewardResult AddRewards(int expGain = 0, int soulGain = 0)
        {
            exp += expGain;
            soul += soulGain;

            int levelUpCount = 0;

            // 여러 레벨이 한 번에 오르는 경우 처리
            while (exp >= GetRequiredExp())
            {
                int requiredExp = GetRequiredExp();
                exp -= requiredExp;
                level++;
                levelUpCount++;
            }

            if (levelUpCount > 0)
            {
                int gainedPoints = levelUpCount * settings.UpgradePointPerLevel;
                upgradePoints += gainedPoints;
                Debug.Log($"레벨 업! Lv.{level} / 업그레이드 포인트 +{gainedPoints}", this);
            }

            return new RewardResult(levelUpCount, GetState());
        }

        public bool SpendUpgradePoint()
        {
            if (upgradePoints <= 0)
            {
                return false;
            }

            upgradePoints--;
            return true;
        }

        public PlayerSnapshot GetState()
        {
            return new PlayerSnapshot(level, exp, GetRequiredExp(), soul, upgradePoints);
        }

        private int GetRequiredExp()
        {
            double requiredExp = settings.BaseRequiredExp * Math.Pow(level, settings.ExpGrowth);
            return (int)Math.Floor(requiredExp + 0.5d);
        }
    }

    public readonly struct RewardResult
    {
        public RewardResult(int levelUpCount, PlayerSnapshot state)
        {
            LevelUpCount = levelUpCount;
            State = state;
        }

        public int LevelUpCount { get; }
        public PlayerSnapshot State { get; }
    }

    public readonly struct PlayerSnapshot
    {
        public PlayerSnapshot(int level, int exp, int requiredExp, int soul, int upgradePoints)
        {
            Level = level;
            Exp = exp;
            RequiredExp = requiredExp;
            Soul = soul;
            UpgradePoints = upgradePoints;
        }

        public int Level { get; }
        public int Exp { get; }
        public int RequiredExp { get; }
        public int Soul { get; }
        public int UpgradePoints { get; }
    }
}
