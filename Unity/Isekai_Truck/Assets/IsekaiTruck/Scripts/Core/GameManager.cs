using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Input;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using IsekaiTruck.Spawn;
using IsekaiTruck.Truck;
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

        private void Awake()
        {
            if (config == null || playerTarget == null || joystickInput == null || truckController == null || cameraController == null || worldManager == null || monsterManager == null || playerState == null || monsterSpawner == null)
            {
                Debug.LogError("GameManager references are not configured.", this);
                enabled = false;
                return;
            }

            truckController.Initialize(config);
            cameraController.Initialize(config, playerTarget);
            joystickInput.SetViewport(cameraController.ViewportRect);
            worldManager.Initialize(config, playerTarget, cameraController.TargetCamera);
            monsterManager.Initialize(config, playerTarget);
            playerState.Initialize(config);
            monsterManager.MonsterDefeated += HandleMonsterDefeated;
            monsterSpawner.Initialize(config, monsterManager, playerTarget);
            monsterSpawner.FillInitial();
        }

        private void Update()
        {
            truckController.UpdateTruck(joystickInput.Move);

            float zoomMultiplier = cameraController.UpdateCamera();
            joystickInput.SetViewport(cameraController.ViewportRect);
            worldManager.UpdateWorld(zoomMultiplier);
            monsterManager.UpdateMonsters();
            monsterSpawner.UpdateSpawner(Time.realtimeSinceStartup * 1000f);
        }

        private void HandleMonsterDefeated(MonsterData type)
        {
            playerState.AddRewards(type.Exp, type.Soul);
            Debug.Log($"경험치 +{type.Exp}, 영혼 +{type.Soul}", this);
        }

        private void OnDestroy()
        {
            if (monsterManager != null)
            {
                monsterManager.MonsterDefeated -= HandleMonsterDefeated;
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
#endif
    }
}
