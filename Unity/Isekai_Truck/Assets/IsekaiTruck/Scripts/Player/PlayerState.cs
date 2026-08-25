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
        private float expRewardRemainder;
        private float soulRewardRemainder;

        public int Level => level;
        public int Exp => exp;
        public int Soul => soul;
        public int UpgradePoints => upgradePoints;
        public int RequiredExp => GetRequiredExp();
        public float ExpRewardRemainder => expRewardRemainder;
        public float SoulRewardRemainder => soulRewardRemainder;

        public event Action<PlayerSnapshot> StateChanged;

        public void Initialize(GameConfig gameConfig)
        {
            settings = gameConfig.Player;
            level = settings.StartLevel;
            exp = settings.StartExp;
            soul = settings.StartSoul;
            upgradePoints = 0;
            expRewardRemainder = 0f;
            soulRewardRemainder = 0f;
        }

        public RewardResult AddRewards(int expGain = 0, int soulGain = 0, float rewardMultiplier = 1f)
        {
            return AddRewards(expGain, soulGain, rewardMultiplier, rewardMultiplier);
        }

        public RewardResult AddRewards(int expGain, int soulGain, float expMultiplier, float soulMultiplier)
        {
            float scaledExp = Mathf.Max(0, expGain) * Mathf.Max(0f, expMultiplier) + expRewardRemainder;
            float scaledSoul = Mathf.Max(0, soulGain) * Mathf.Max(0f, soulMultiplier) + soulRewardRemainder;
            int appliedExp = Mathf.FloorToInt(scaledExp + 0.00001f);
            int appliedSoul = Mathf.FloorToInt(scaledSoul + 0.00001f);

            expRewardRemainder = scaledExp - appliedExp;
            soulRewardRemainder = scaledSoul - appliedSoul;
            exp += appliedExp;
            soul += appliedSoul;

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

            PlayerSnapshot state = GetState();
            StateChanged?.Invoke(state);
            return new RewardResult(levelUpCount, appliedExp, appliedSoul, state);
        }

        public void ResetForRebirth()
        {
            level = settings.StartLevel;
            exp = settings.StartExp;
            upgradePoints = 0;
            StateChanged?.Invoke(GetState());
        }

        public void ForfeitCurrentExperience()
        {
            exp = 0;
            expRewardRemainder = 0f;
            StateChanged?.Invoke(GetState());
        }

        public void RestoreState(int savedLevel, int savedExp, int savedSoul, int savedUpgradePoints, float savedExpRemainder, float savedSoulRemainder)
        {
            level = Mathf.Max(settings.StartLevel, savedLevel);
            exp = Mathf.Max(0, savedExp);
            soul = Mathf.Max(0, savedSoul);
            upgradePoints = Mathf.Max(0, savedUpgradePoints);
            expRewardRemainder = Mathf.Clamp(savedExpRemainder, 0f, 0.99999f);
            soulRewardRemainder = Mathf.Clamp(savedSoulRemainder, 0f, 0.99999f);
            StateChanged?.Invoke(GetState());
        }

        public bool SpendUpgradePoint()
        {
            if (upgradePoints <= 0)
            {
                return false;
            }

            upgradePoints--;
            StateChanged?.Invoke(GetState());
            return true;
        }

        public void AddSoul(int soulGain)
        {
            if (soulGain <= 0)
            {
                return;
            }

            soul += soulGain;
            StateChanged?.Invoke(GetState());
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
        public RewardResult(int levelUpCount, int appliedExp, int appliedSoul, PlayerSnapshot state)
        {
            LevelUpCount = levelUpCount;
            AppliedExp = appliedExp;
            AppliedSoul = appliedSoul;
            State = state;
        }

        public int LevelUpCount { get; }
        public int AppliedExp { get; }
        public int AppliedSoul { get; }
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
