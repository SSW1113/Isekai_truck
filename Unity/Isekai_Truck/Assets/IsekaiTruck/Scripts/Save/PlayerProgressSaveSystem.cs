using System;
using System.Collections.Generic;
using IsekaiTruck.Blessings;
using IsekaiTruck.Player;
using IsekaiTruck.Rebirth;
using IsekaiTruck.Truck;
using IsekaiTruck.Upgrades;
using UnityEngine;

namespace IsekaiTruck.Save
{
    [DisallowMultipleComponent]
    public sealed class PlayerProgressSaveSystem : MonoBehaviour
    {
        private const string SaveKey = "IsekaiTruck.PlayerProgress.v1";
        private const float AutosaveInterval = 5f;

        private PlayerState playerState;
        private TruckController truckController;
        private RebirthSystem rebirthSystem;
        private BlessingSystem blessingSystem;
        private TruckUpgradeSystem upgradeSystem;
        private float nextAutosaveTime;
        private bool isInitialized;
        private bool isDirty;
        private string saveKey = SaveKey;
        private Vector3 lastSavedTruckPosition;
        private float lastSavedTruckYaw;

        public void Initialize(PlayerState player, TruckController truck, RebirthSystem rebirth, BlessingSystem blessings, TruckUpgradeSystem upgrades)
        {
            playerState = player;
            truckController = truck;
            rebirthSystem = rebirth;
            blessingSystem = blessings;
            upgradeSystem = upgrades;

            Load();

            playerState.StateChanged += HandleStateChanged;
            rebirthSystem.StateChanged += HandleRebirthStateChanged;
            blessingSystem.StateChanged += HandleStateChanged;
            upgradeSystem.UpgradeApplied += HandleUpgradeApplied;
            nextAutosaveTime = Time.realtimeSinceStartup + AutosaveInterval;
            lastSavedTruckPosition = truckController.transform.position;
            lastSavedTruckYaw = truckController.transform.eulerAngles.y;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || Time.realtimeSinceStartup < nextAutosaveTime)
            {
                return;
            }

            Vector3 position = truckController.transform.position;
            bool hasMoved = (position - lastSavedTruckPosition).sqrMagnitude > 0.0001f || Mathf.Abs(Mathf.DeltaAngle(lastSavedTruckYaw, truckController.transform.eulerAngles.y)) > 0.01f;
            if (isDirty || hasMoved)
            {
                Save();
            }

            nextAutosaveTime = Time.realtimeSinceStartup + AutosaveInterval;
        }

        public void Save()
        {
            if (playerState == null || truckController == null || rebirthSystem == null || blessingSystem == null)
            {
                return;
            }

            PlayerSnapshot player = playerState.GetState();
            TruckController.TruckStats truck = truckController.GetStats();
            Vector3 position = truckController.transform.position;

            PlayerProgressData data = new PlayerProgressData
            {
                version = 1,
                level = player.Level,
                exp = player.Exp,
                soul = player.Soul,
                upgradePoints = player.UpgradePoints,
                expRewardRemainder = playerState.ExpRewardRemainder,
                soulRewardRemainder = playerState.SoulRewardRemainder,
                speedLevel = truck.SpeedLevel,
                sizeLevel = truck.SizeLevel,
                truckPositionX = position.x,
                truckPositionZ = position.z,
                truckYaw = truckController.transform.eulerAngles.y,
                totalRebirthCount = rebirthSystem.TotalRebirthCount,
                maxRebirthCount = rebirthSystem.MaxRebirthCount,
                maxUnlockedTierIndex = rebirthSystem.MaxUnlockedTierIndex,
                pendingTierIndex = rebirthSystem.PendingTierIndex,
                ownedBlessings = blessingSystem.GetOwnedSnapshot(),
                pendingCandidateIds = blessingSystem.GetPendingCandidateIds()
            };

            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            isDirty = false;
            lastSavedTruckPosition = position;
            lastSavedTruckYaw = truckController.transform.eulerAngles.y;
        }

        public bool Load()
        {
            if (!PlayerPrefs.HasKey(saveKey))
            {
                return false;
            }

            string json = PlayerPrefs.GetString(saveKey);
            PlayerProgressData data = JsonUtility.FromJson<PlayerProgressData>(json);
            if (data == null || data.version != 1)
            {
                Debug.LogWarning("Player progress save is invalid or unsupported.", this);
                return false;
            }

            playerState.RestoreState(data.level, data.exp, data.soul, data.upgradePoints, data.expRewardRemainder, data.soulRewardRemainder);
            truckController.RestoreProgress(data.speedLevel, data.sizeLevel, new Vector3(data.truckPositionX, 0f, data.truckPositionZ), data.truckYaw);
            blessingSystem.RestoreState(data.ownedBlessings, data.pendingCandidateIds);
            rebirthSystem.RestoreState(data.totalRebirthCount, data.maxRebirthCount, data.maxUnlockedTierIndex, data.pendingTierIndex);
            return true;
        }

        private void HandleStateChanged()
        {
            isDirty = true;
        }

        private void HandleStateChanged(PlayerSnapshot state)
        {
            isDirty = true;
        }

        private void HandleRebirthStateChanged()
        {
            Save();
        }

        private void HandleUpgradeApplied(TruckUpgradeResult result)
        {
            isDirty = true;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnDestroy()
        {
            if (playerState != null) playerState.StateChanged -= HandleStateChanged;
            if (rebirthSystem != null) rebirthSystem.StateChanged -= HandleRebirthStateChanged;
            if (blessingSystem != null) blessingSystem.StateChanged -= HandleStateChanged;
            if (upgradeSystem != null) upgradeSystem.UpgradeApplied -= HandleUpgradeApplied;
        }

#if UNITY_EDITOR
        public void SetSaveKeyForVerification(string verificationSaveKey)
        {
            saveKey = verificationSaveKey;
        }

        public static void DeleteSaveForVerification(string verificationSaveKey)
        {
            PlayerPrefs.DeleteKey(verificationSaveKey);
            PlayerPrefs.Save();
        }
#endif
    }

    [Serializable]
    public sealed class PlayerProgressData
    {
        public int version;
        public int level;
        public int exp;
        public int soul;
        public int upgradePoints;
        public float expRewardRemainder;
        public float soulRewardRemainder;
        public int speedLevel;
        public int sizeLevel;
        public float truckPositionX;
        public float truckPositionZ;
        public float truckYaw;
        public int totalRebirthCount;
        public int maxRebirthCount;
        public int maxUnlockedTierIndex;
        public int pendingTierIndex = -1;
        public List<OwnedBlessingData> ownedBlessings = new List<OwnedBlessingData>();
        public List<string> pendingCandidateIds = new List<string>();
    }
}
