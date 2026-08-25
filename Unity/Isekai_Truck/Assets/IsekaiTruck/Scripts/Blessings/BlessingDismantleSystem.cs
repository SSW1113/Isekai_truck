using System;
using IsekaiTruck.Config;
using IsekaiTruck.Player;
using UnityEngine;

namespace IsekaiTruck.Blessings
{
    [DisallowMultipleComponent]
    public sealed class BlessingDismantleSystem : MonoBehaviour
    {
        private GameConfig.BlessingSettings settings;
        private BlessingSystem blessingSystem;
        private BlessingLoadoutSystem loadoutSystem;
        private PlayerState playerState;

        public event Action<BlessingDismantleResult> BlessingDismantled;

        public void Initialize(GameConfig gameConfig, BlessingSystem blessings, BlessingLoadoutSystem loadout, PlayerState player)
        {
            settings = gameConfig.Blessing;
            blessingSystem = blessings;
            loadoutSystem = loadout;
            playerState = player;
        }

        public int GetAvailableCount(string blessingId)
        {
            return Mathf.Max(0, blessingSystem.GetOwnedCount(blessingId) - loadoutSystem.GetEquippedCount(blessingId));
        }

        public int GetDismantleSoul(BlessingGrade grade)
        {
            return grade switch
            {
                BlessingGrade.C => settings.CDismantleSoul,
                BlessingGrade.U => settings.UDismantleSoul,
                BlessingGrade.R => settings.RDismantleSoul,
                BlessingGrade.SR => settings.SrDismantleSoul,
                _ => 0
            };
        }

        public bool TryDismantle(string blessingId, out BlessingDismantleResult result)
        {
            result = default;
            BlessingDefinition definition = blessingSystem.FindDefinition(blessingId);
            if (definition == null || GetAvailableCount(blessingId) <= 0)
            {
                return false;
            }

            int soul = GetDismantleSoul(definition.Grade);
            if (!blessingSystem.TryRemoveOwned(blessingId))
            {
                return false;
            }

            playerState.AddSoul(soul);
            result = new BlessingDismantleResult(definition, soul);
            BlessingDismantled?.Invoke(result);
            return true;
        }
    }

    public readonly struct BlessingDismantleResult
    {
        public BlessingDismantleResult(BlessingDefinition blessing, int soul)
        {
            Blessing = blessing;
            Soul = soul;
        }

        public BlessingDefinition Blessing { get; }
        public int Soul { get; }
    }
}
