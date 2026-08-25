using System;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Upgrades
{
    [DisallowMultipleComponent]
    public sealed class TruckUpgradeSystem : MonoBehaviour
    {
        private PlayerState playerState;
        private TruckController truckController;

        public event Action<TruckUpgradeResult> UpgradeApplied;

        public void Initialize(PlayerState state, TruckController controller)
        {
            playerState = state;
            truckController = controller;
        }

        public bool TryUpgradeSpeed()
        {
            return TryUpgrade(TruckUpgradeType.Speed);
        }

        public bool TryUpgradeSize()
        {
            return TryUpgrade(TruckUpgradeType.Size);
        }

        public bool TryUpgrade(TruckUpgradeType upgradeType)
        {
            if (playerState == null || truckController == null)
            {
                Debug.LogError("TruckUpgradeSystem is not initialized.", this);
                return false;
            }

            if (upgradeType != TruckUpgradeType.Speed && upgradeType != TruckUpgradeType.Size)
            {
                return false;
            }

            if (!playerState.SpendUpgradePoint())
            {
                return false;
            }

            if (upgradeType == TruckUpgradeType.Speed)
            {
                truckController.UpgradeSpeed();
            }
            else
            {
                truckController.UpgradeSize();
            }

            TruckUpgradeResult result = new TruckUpgradeResult(
                upgradeType,
                playerState.GetState(),
                truckController.GetStats()
            );

            UpgradeApplied?.Invoke(result);
            return true;
        }
    }

    public enum TruckUpgradeType
    {
        Speed,
        Size
    }

    public readonly struct TruckUpgradeResult
    {
        public TruckUpgradeResult(
            TruckUpgradeType upgradeType,
            PlayerSnapshot player,
            TruckController.TruckStats truck
        )
        {
            UpgradeType = upgradeType;
            Player = player;
            Truck = truck;
        }

        public TruckUpgradeType UpgradeType { get; }
        public PlayerSnapshot Player { get; }
        public TruckController.TruckStats Truck { get; }
    }
}
