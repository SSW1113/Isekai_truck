using System;
using IsekaiTruck.Blessings;
using IsekaiTruck.Config;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Rebirth
{
    [DisallowMultipleComponent]
    public sealed class RebirthSystem : MonoBehaviour
    {
        private GameConfig.RebirthSettings settings;
        private PlayerState playerState;
        private TruckController truckController;
        private BlessingSystem blessingSystem;
        private int totalRebirthCount;
        private int maxRebirthCount;
        private int maxUnlockedTierIndex;
        private int pendingTierIndex = -1;

        public int TotalRebirthCount => totalRebirthCount;
        public int MaxRebirthCount => maxRebirthCount;
        public int MaxUnlockedTierIndex => maxUnlockedTierIndex;
        public int PendingTierIndex => pendingTierIndex;
        public bool HasPendingRebirth => pendingTierIndex >= 0 && blessingSystem.HasPendingCandidates;
        public float RewardMultiplier => 1f + maxRebirthCount * settings.RewardMultiplierPerMaxRebirth;
        public GameConfig.RebirthTierSettings[] Tiers => settings.Tiers;

        public event Action StateChanged;
        public event Action<RebirthResult> RebirthCompleted;

        public void Initialize(GameConfig gameConfig, PlayerState state, TruckController truck, BlessingSystem blessings)
        {
            settings = gameConfig.Rebirth;
            playerState = state;
            truckController = truck;
            blessingSystem = blessings;
            totalRebirthCount = 0;
            maxRebirthCount = 0;
            maxUnlockedTierIndex = 0;
            pendingTierIndex = -1;
        }

        public bool CanBeginRebirth(int tierIndex)
        {
            return !HasPendingRebirth && tierIndex >= 0 && tierIndex <= maxUnlockedTierIndex && tierIndex < settings.Tiers.Length && playerState.Level >= settings.Tiers[tierIndex].RequiredLevel;
        }

        public bool BeginRebirth(int tierIndex)
        {
            if (!CanBeginRebirth(tierIndex))
            {
                return false;
            }

            if (!blessingSystem.CreateCandidates(settings.Tiers[tierIndex], settings.BlessingCandidateCount))
            {
                return false;
            }

            pendingTierIndex = tierIndex;
            StateChanged?.Invoke();
            return true;
        }

        public bool CompleteRebirth(int candidateIndex, out RebirthResult result)
        {
            result = default;
            if (!HasPendingRebirth)
            {
                return false;
            }

            int completedTierIndex = pendingTierIndex;
            BlessingDefinition blessing = blessingSystem.ChooseCandidate(candidateIndex);
            if (blessing == null)
            {
                return false;
            }

            bool isMaximumTier = completedTierIndex == maxUnlockedTierIndex;
            totalRebirthCount++;
            if (isMaximumTier)
            {
                maxRebirthCount++;
                if (maxUnlockedTierIndex < settings.Tiers.Length - 1)
                {
                    maxUnlockedTierIndex++;
                }
            }

            pendingTierIndex = -1;
            playerState.ResetForRebirth();
            truckController.ResetUpgrades();

            result = new RebirthResult(completedTierIndex, isMaximumTier, RewardMultiplier, blessing);
            StateChanged?.Invoke();
            RebirthCompleted?.Invoke(result);
            return true;
        }

        public void RestoreState(int savedTotalCount, int savedMaxCount, int savedMaxUnlockedTierIndex, int savedPendingTierIndex)
        {
            totalRebirthCount = Mathf.Max(0, savedTotalCount);
            maxRebirthCount = Mathf.Max(0, savedMaxCount);
            maxUnlockedTierIndex = Mathf.Clamp(savedMaxUnlockedTierIndex, 0, settings.Tiers.Length - 1);
            pendingTierIndex = savedPendingTierIndex >= 0 && savedPendingTierIndex <= maxUnlockedTierIndex
                ? savedPendingTierIndex
                : -1;
            StateChanged?.Invoke();
        }
    }

    public readonly struct RebirthResult
    {
        public RebirthResult(int tierIndex, bool isMaximumTier, float rewardMultiplier, BlessingDefinition blessing)
        {
            TierIndex = tierIndex;
            IsMaximumTier = isMaximumTier;
            RewardMultiplier = rewardMultiplier;
            Blessing = blessing;
        }

        public int TierIndex { get; }
        public bool IsMaximumTier { get; }
        public float RewardMultiplier { get; }
        public BlessingDefinition Blessing { get; }
    }
}
