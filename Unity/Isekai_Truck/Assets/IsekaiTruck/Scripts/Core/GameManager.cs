using IsekaiTruck.Blessings;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Enemies;
using IsekaiTruck.Input;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using IsekaiTruck.Rebirth;
using IsekaiTruck.Save;
using IsekaiTruck.Spawn;
using IsekaiTruck.Truck;
using IsekaiTruck.Upgrades;
using IsekaiTruck.UI;
using IsekaiTruck.Wanted;
using IsekaiTruck.World;
using UnityEngine;

namespace IsekaiTruck.Core
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private JoystickInput joystickInput;
        [SerializeField] private TruckController truckController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private WorldManager worldManager;
        [SerializeField] private MonsterManager monsterManager;
        [SerializeField] private PlayerState playerState;
        [SerializeField] private MonsterSpawner monsterSpawner;
        [SerializeField] private TruckUpgradeSystem truckUpgradeSystem;
        [SerializeField] private GameUIController gameUIController;
        [SerializeField] private BlessingSystem blessingSystem;
        [SerializeField] private RebirthSystem rebirthSystem;
        [SerializeField] private PlayerProgressSaveSystem saveSystem;
        [SerializeField] private RebirthUIController rebirthUIController;
        [SerializeField] private BlessingLoadoutSystem blessingLoadoutSystem;
        [SerializeField] private BlessingDismantleSystem blessingDismantleSystem;
        [SerializeField] private BlessingEffectSystem blessingEffectSystem;
        [SerializeField] private BlessingInput blessingInput;
        [SerializeField] private BlessingInventoryUIController blessingInventoryUIController;
        [SerializeField] private WantedLevelSystem wantedLevelSystem;
        [SerializeField] private WantedLevelUIController wantedLevelUIController;
        [SerializeField] private TruckHealthController truckHealthController;
        [SerializeField] private TruckDamageFlash truckDamageFlash;
        [SerializeField] private TruckHealthUIController truckHealthUIController;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private EnemyWarningUIController enemyWarningUIController;

        private Vector3 truckRespawnPosition;
        private float truckRespawnYaw;

        private void Awake()
        {
            if (config == null || playerTarget == null || joystickInput == null || truckController == null || cameraController == null || worldManager == null || monsterManager == null || playerState == null || monsterSpawner == null || truckUpgradeSystem == null || gameUIController == null || blessingSystem == null || rebirthSystem == null || saveSystem == null || rebirthUIController == null || blessingLoadoutSystem == null || blessingDismantleSystem == null || blessingEffectSystem == null || blessingInput == null || blessingInventoryUIController == null || wantedLevelSystem == null || wantedLevelUIController == null || truckHealthController == null || truckDamageFlash == null || truckHealthUIController == null || enemyManager == null || enemySpawner == null || enemyWarningUIController == null)
            {
                Debug.LogError("GameManager references are not configured.", this);
                enabled = false;
                return;
            }

            truckRespawnPosition = playerTarget.position;
            truckRespawnYaw = playerTarget.eulerAngles.y;
            truckController.Initialize(config);
            truckHealthController.Initialize(config, truckDamageFlash);
            playerState.Initialize(config);
            blessingSystem.Initialize();
            blessingLoadoutSystem.Initialize(config, blessingSystem);
            wantedLevelSystem.Initialize(config);
            rebirthSystem.Initialize(config, playerState, truckController, blessingSystem);
            truckUpgradeSystem.Initialize(playerState, truckController);
            blessingDismantleSystem.Initialize(config, blessingSystem, blessingLoadoutSystem, playerState);
            saveSystem.Initialize(playerState, truckController, rebirthSystem, blessingSystem, blessingLoadoutSystem, wantedLevelSystem, truckHealthController, truckUpgradeSystem);
            cameraController.Initialize(config, playerTarget);
            joystickInput.SetViewport(cameraController.ViewportRect);
            worldManager.Initialize(config, playerTarget, cameraController.TargetCamera);
            monsterManager.Initialize(config, playerTarget);
            enemyManager.Initialize(config, playerTarget, truckHealthController);
            enemyWarningUIController.Initialize(config, enemyManager, cameraController.TargetCamera, playerTarget);
            blessingEffectSystem.Initialize(blessingLoadoutSystem, truckController, cameraController, monsterManager, enemyManager);
            blessingInput.Initialize(blessingEffectSystem);
            gameUIController.Initialize(playerState, truckController, truckUpgradeSystem, joystickInput, cameraController);
            rebirthUIController.Initialize(rebirthSystem, blessingSystem, playerState, joystickInput, cameraController);
            blessingInventoryUIController.Initialize(blessingSystem, blessingLoadoutSystem, blessingDismantleSystem, blessingEffectSystem, joystickInput, cameraController);
            wantedLevelUIController.Initialize(wantedLevelSystem);
            truckHealthUIController.Initialize(truckHealthController);
            monsterManager.MonsterDefeated += HandleMonsterDefeated;
            truckHealthController.Defeated += HandleTruckDefeated;
            monsterSpawner.Initialize(config, monsterManager, playerTarget);
            monsterSpawner.FillInitial();
            enemySpawner.Initialize(config, enemyManager, wantedLevelSystem, playerTarget);
            enemySpawner.FillInitial();
        }

        private void Update()
        {
            if (gameUIController.IsUpgradePanelOpen || rebirthUIController.IsPanelOpen || blessingInventoryUIController.IsPanelOpen)
            {
                enemyWarningUIController.Hide();
                return;
            }

            float deltaTime = Time.deltaTime;
            blessingInput.ReadInput();
            blessingEffectSystem.UpdateEffects(deltaTime);
            truckHealthController.UpdateHealth(deltaTime);
            truckController.UpdateTruck(joystickInput.Move, deltaTime);

            float zoomMultiplier = cameraController.UpdateCamera(deltaTime);
            joystickInput.SetViewport(cameraController.ViewportRect);
            gameUIController.SetViewport(cameraController.ViewportRect);
            rebirthUIController.SetViewport(cameraController.ViewportRect);
            blessingInventoryUIController.SetViewport(cameraController.ViewportRect);
            blessingInventoryUIController.RefreshRuntime();
            worldManager.UpdateWorld(zoomMultiplier);
            monsterManager.UpdateMonsters(deltaTime);
            enemyManager.UpdateEnemies(deltaTime);
            enemyWarningUIController.UpdateWarning(deltaTime);
            if (!blessingEffectSystem.IsWorldTimeStopped)
            {
                monsterSpawner.UpdateSpawner(Time.realtimeSinceStartup * 1000f);
                enemySpawner.UpdateSpawner(Time.realtimeSinceStartup * 1000f);
            }
        }

        private void HandleMonsterDefeated(MonsterData type)
        {
            float rebirthMultiplier = rebirthSystem.RewardMultiplier;
            RewardResult reward = playerState.AddRewards(type.Exp, type.Soul, rebirthMultiplier * blessingEffectSystem.ExperienceMultiplier, rebirthMultiplier);
            wantedLevelSystem.RegisterKill();
            Debug.Log($"경험치 +{reward.AppliedExp}, 영혼 +{reward.AppliedSoul}", this);
        }

        private void HandleTruckDefeated()
        {
            playerState.ForfeitCurrentExperience();
            truckController.Respawn(truckRespawnPosition, truckRespawnYaw);
            truckHealthController.Respawn();
            Debug.Log("트럭이 파괴되어 보유 경험치를 잃고 리스폰했습니다.", this);
        }

        private void OnDestroy()
        {
            if (monsterManager != null)
            {
                monsterManager.MonsterDefeated -= HandleMonsterDefeated;
            }

            if (truckHealthController != null)
            {
                truckHealthController.Defeated -= HandleTruckDefeated;
            }
        }

#if UNITY_EDITOR
        public void SetConfig(GameConfig gameConfig)
        {
            config = gameConfig;
        }

        public void SetTruckSystems(JoystickInput input, TruckController controller)
        {
            joystickInput = input;
            truckController = controller;
        }

        public void SetMonsterSystem(MonsterManager manager)
        {
            monsterManager = manager;
        }

        public void SetPlayerSystem(PlayerState state)
        {
            playerState = state;
        }

        public void SetSpawnSystem(MonsterSpawner spawner)
        {
            monsterSpawner = spawner;
        }

        public void SetUpgradeSystem(TruckUpgradeSystem upgradeSystem)
        {
            truckUpgradeSystem = upgradeSystem;
        }

        public void SetUISystem(GameUIController uiController)
        {
            gameUIController = uiController;
        }

        public void SetRebirthSystems(BlessingSystem blessings, RebirthSystem rebirth, PlayerProgressSaveSystem progressSave, RebirthUIController rebirthUI)
        {
            blessingSystem = blessings;
            rebirthSystem = rebirth;
            saveSystem = progressSave;
            rebirthUIController = rebirthUI;
        }

        public void SetBlessingSkillSystems(
            BlessingLoadoutSystem loadout,
            BlessingDismantleSystem dismantle,
            BlessingEffectSystem effects,
            BlessingInput input,
            BlessingInventoryUIController inventoryUI
        )
        {
            blessingLoadoutSystem = loadout;
            blessingDismantleSystem = dismantle;
            blessingEffectSystem = effects;
            blessingInput = input;
            blessingInventoryUIController = inventoryUI;
        }

        public void SetWantedLevelSystems(WantedLevelSystem wanted, WantedLevelUIController wantedUI)
        {
            wantedLevelSystem = wanted;
            wantedLevelUIController = wantedUI;
        }

        public void SetEnemySystems(
            TruckHealthController health,
            TruckDamageFlash damageFlash,
            TruckHealthUIController healthUI,
            EnemyManager manager,
            EnemySpawner spawner,
            EnemyWarningUIController warningUI
        )
        {
            truckHealthController = health;
            truckDamageFlash = damageFlash;
            truckHealthUIController = healthUI;
            enemyManager = manager;
            enemySpawner = spawner;
            enemyWarningUIController = warningUI;
        }
#endif
    }
}
