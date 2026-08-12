using UnityEngine.UI;
using UnityEngine;

namespace IsekaiTruck.UI
{
    public sealed class BottomMenuUI : MonoBehaviour
    {
        [SerializeField] private Button rebirthButton;
        [SerializeField] private Button truckUpgradeButton;
        [SerializeField] private Button driveButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button settingsButton;

        public void Configure(
            Button rebirth,
            Button truckUpgrade,
            Button drive,
            Button collection,
            Button settings)
        {
            rebirthButton = rebirth;
            truckUpgradeButton = truckUpgrade;
            driveButton = drive;
            collectionButton = collection;
            settingsButton = settings;
        }

        private void Awake()
        {
            rebirthButton?.onClick.AddListener(OnRebirthClicked);
            truckUpgradeButton?.onClick.AddListener(OnTruckUpgradeClicked);
            driveButton?.onClick.AddListener(OnDriveClicked);
            collectionButton?.onClick.AddListener(OnCollectionClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDestroy()
        {
            rebirthButton?.onClick.RemoveListener(OnRebirthClicked);
            truckUpgradeButton?.onClick.RemoveListener(OnTruckUpgradeClicked);
            driveButton?.onClick.RemoveListener(OnDriveClicked);
            collectionButton?.onClick.RemoveListener(OnCollectionClicked);
            settingsButton?.onClick.RemoveListener(OnSettingsClicked);
        }

        public void OnRebirthClicked() => Debug.Log("Rebirth button clicked");
        public void OnTruckUpgradeClicked() => Debug.Log("Truck Upgrade button clicked");
        public void OnDriveClicked() => Debug.Log("Drive button clicked");
        public void OnCollectionClicked() => Debug.Log("Collection button clicked");
        public void OnSettingsClicked() => Debug.Log("Settings button clicked");
    }
}
