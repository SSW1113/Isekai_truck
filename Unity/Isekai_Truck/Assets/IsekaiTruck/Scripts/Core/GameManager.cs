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
using IsekaiTruck.Visuals;
using UnityEngine;

namespace IsekaiTruck.Core
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private JoystickInput joystickInput;
        [SerializeField] private PlayerMoveInput playerMoveInput;
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
        [SerializeField] private WorldTravelSystem worldTravelSystem;
        [SerializeField] private WorldTravelUIController worldTravelUIController;
        [SerializeField] private CollisionFeedbackController collisionFeedbackController;
        [SerializeField] private SoulRewardFlyUI soulRewardFlyUI;

        private Vector3 truckRespawnPosition;
        private float truckRespawnYaw;

        public bool IsMenuPaused =>
            (gameUIController != null && gameUIController.IsUpgradePanelOpen) ||
            (rebirthUIController != null && rebirthUIController.IsPanelOpen) ||
            (blessingInventoryUIController != null && blessingInventoryUIController.IsPanelOpen) ||
            (worldTravelUIController != null && worldTravelUIController.IsPanelOpen);

        private void Awake()
        {
            if (config == null || playerTarget == null || joystickInput == null || playerMoveInput == null || truckController == null || cameraController == null || worldManager == null || monsterManager == null || playerState == null || monsterSpawner == null || truckUpgradeSystem == null || gameUIController == null || blessingSystem == null || rebirthSystem == null || saveSystem == null || rebirthUIController == null || blessingLoadoutSystem == null || blessingDismantleSystem == null || blessingEffectSystem == null || blessingInput == null || blessingInventoryUIController == null || wantedLevelSystem == null || wantedLevelUIController == null || truckHealthController == null || truckDamageFlash == null || truckHealthUIController == null || enemyManager == null || enemySpawner == null || enemyWarningUIController == null || worldTravelSystem == null || worldTravelUIController == null || collisionFeedbackController == null || soulRewardFlyUI == null)
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
            worldTravelSystem.Initialize(config, wantedLevelSystem);
            rebirthSystem.Initialize(config, playerState, truckController, blessingSystem);
            truckUpgradeSystem.Initialize(playerState, truckController);
            blessingDismantleSystem.Initialize(config, blessingSystem, blessingLoadoutSystem, playerState);
            saveSystem.Initialize(playerState, truckController, rebirthSystem, blessingSystem, blessingLoadoutSystem, wantedLevelSystem, truckHealthController, worldTravelSystem, truckUpgradeSystem);
            cameraController.Initialize(config, playerTarget);
            joystickInput.SetViewport(cameraController.ViewportRect);
            worldManager.Initialize(config, playerTarget, cameraController.TargetCamera, worldTravelSystem.CurrentWorld);
            monsterManager.Initialize(config, playerTarget);
            enemyManager.Initialize(config, playerTarget, truckHealthController);
            enemyWarningUIController.Initialize(config, enemyManager, cameraController.TargetCamera, playerTarget);
            blessingEffectSystem.Initialize(blessingLoadoutSystem, truckController, cameraController, monsterManager, enemyManager);
            blessingInput.Initialize(blessingEffectSystem);
            gameUIController.Initialize(playerState, truckController, truckUpgradeSystem, joystickInput, cameraController);
            rebirthUIController.Initialize(rebirthSystem, blessingSystem, playerState, joystickInput, cameraController);
            blessingInventoryUIController.Initialize(blessingSystem, blessingLoadoutSystem, blessingDismantleSystem, blessingEffectSystem, joystickInput, cameraController);
            wantedLevelUIController.Initialize(wantedLevelSystem);
            worldTravelUIController.Initialize(worldTravelSystem, joystickInput, cameraController);
            truckHealthUIController.Initialize(truckHealthController);
            collisionFeedbackController.Initialize();
            soulRewardFlyUI.Initialize(cameraController);
            monsterManager.MonsterDefeatedDetailed += HandleMonsterDefeated;
            monsterManager.MonsterCollisionBatchCompleted += HandleMonsterCollisionBatch;
            truckHealthController.DamageTaken += HandleTruckDamageTaken;
            truckHealthController.Defeated += HandleTruckDefeated;
            worldTravelSystem.WorldChanged += HandleWorldChanged;
            monsterSpawner.Initialize(config, monsterManager, playerTarget);
            monsterSpawner.FillInitial();
            enemySpawner.Initialize(config, enemyManager, wantedLevelSystem, playerTarget);
            enemySpawner.FillInitial();
        }

        private void Update()
        {
            if (IsMenuPaused)
            {
                enemyWarningUIController.Hide();
                return;
            }

            float deltaTime = Time.deltaTime;
            blessingInput.ReadInput();
            blessingEffectSystem.UpdateEffects(deltaTime);
            truckHealthController.UpdateHealth(deltaTime);
            truckController.UpdateTruck(playerMoveInput.Move, deltaTime);

            float zoomMultiplier = cameraController.UpdateCamera(deltaTime);
            joystickInput.SetViewport(cameraController.ViewportRect);
            gameUIController.SetViewport(cameraController.ViewportRect);
            rebirthUIController.SetViewport(cameraController.ViewportRect);
            blessingInventoryUIController.SetViewport(cameraController.ViewportRect);
            worldTravelUIController.SetViewport(cameraController.ViewportRect);
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

        private void HandleMonsterDefeated(MonsterDefeatContext defeat)
        {
            float rebirthMultiplier = rebirthSystem.RewardMultiplier;
            gameUIController.BeginDeferredSoulReward();
            RewardResult reward = playerState.AddRewards(defeat.Type.Exp, defeat.Type.Soul, rebirthMultiplier * blessingEffectSystem.ExperienceMultiplier, rebirthMultiplier);
            gameUIController.QueueDeferredSoulReward(reward.AppliedSoul);
            collisionFeedbackController.PlayMonsterDefeat(defeat.WorldPosition);
            if (reward.AppliedSoul > 0 && !soulRewardFlyUI.Play(defeat.WorldPosition, reward.AppliedSoul))
            {
                gameUIController.ReleaseDeferredSoul(reward.AppliedSoul);
            }
            wantedLevelSystem.RegisterKill();
            Debug.Log($"경험치 +{reward.AppliedExp}, 영혼 +{reward.AppliedSoul}", this);
        }

        private void HandleMonsterCollisionBatch(MonsterCollisionBatch batch)
        {
            collisionFeedbackController.PlayMonsterCollisionBatch(batch.Count);
        }

        private void HandleTruckDamageTaken(TruckDamageResult result)
        {
            if (result.AppliedDamage > 0)
            {
                cameraController.PlayDamageShake();
            }
        }

        private void HandleTruckDefeated()
        {
            playerState.ForfeitCurrentExperience();
            truckController.Respawn(truckRespawnPosition, truckRespawnYaw);
            truckHealthController.Respawn();
            Debug.Log("트럭이 파괴되어 보유 경험치를 잃고 리스폰했습니다.", this);
        }

        private void HandleWorldChanged(WorldDefinition world)
        {
            worldManager.ApplyWorld(world);
            enemySpawner.ReconcileCount();
            enemyWarningUIController.Hide();
        }

        private void OnDestroy()
        {
            if (monsterManager != null)
            {
                monsterManager.MonsterDefeatedDetailed -= HandleMonsterDefeated;
                monsterManager.MonsterCollisionBatchCompleted -= HandleMonsterCollisionBatch;
            }

            if (truckHealthController != null)
            {
                truckHealthController.DamageTaken -= HandleTruckDamageTaken;
                truckHealthController.Defeated -= HandleTruckDefeated;
            }

            if (worldTravelSystem != null)
            {
                worldTravelSystem.WorldChanged -= HandleWorldChanged;
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

        public void SetMoveInput(PlayerMoveInput moveInput)
        {
            playerMoveInput = moveInput;
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

        public void SetWorldTravelSystems(WorldTravelSystem travelSystem, WorldTravelUIController travelUI)
        {
            worldTravelSystem = travelSystem;
            worldTravelUIController = travelUI;
        }

        public void SetCollisionFeedbackSystems(CollisionFeedbackController collisionFeedback, SoulRewardFlyUI soulRewardUI)
        {
            collisionFeedbackController = collisionFeedback;
            soulRewardFlyUI = soulRewardUI;
        }
#endif
    }
}
