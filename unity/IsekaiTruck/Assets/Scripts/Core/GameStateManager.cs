using IsekaiTruck.Gameplay;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using IsekaiTruck.Spawning;
using IsekaiTruck.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IsekaiTruck.Core
{
    public sealed class GameStateManager : MonoBehaviour
    {
        public enum GameState
        {
            Start,
            Playing,
            Result
        }

        [SerializeField] private TruckController truckController;
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private DrivingTimeManager drivingTimeManager;
        [SerializeField] private Transform monstersParent;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private ResultPanelUI resultPanelUI;

        public GameState CurrentState { get; private set; }

        public void Configure(
            TruckController truck,
            SpawnManager spawner,
            DrivingTimeManager timer,
            Transform monsterContainer,
            GameObject startScreen,
            GameObject hud,
            GameObject resultScreen,
            Button button,
            ResultPanelUI results)
        {
            truckController = truck;
            spawnManager = spawner;
            drivingTimeManager = timer;
            monstersParent = monsterContainer;
            startPanel = startScreen;
            hudPanel = hud;
            resultPanel = resultScreen;
            startButton = button;
            resultPanelUI = results;
        }

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }

            if (drivingTimeManager != null)
            {
                drivingTimeManager.TimeExpired += EndGame;
            }

            if (resultPanelUI != null)
            {
                resultPanelUI.RestartRequested += RestartGame;
            }

            EnterStartState();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }

            if (drivingTimeManager != null)
            {
                drivingTimeManager.TimeExpired -= EndGame;
            }

            if (resultPanelUI != null)
            {
                resultPanelUI.RestartRequested -= RestartGame;
            }
        }

        public void StartGame()
        {
            if (CurrentState == GameState.Playing)
            {
                return;
            }

            CurrentState = GameState.Playing;
            SetActive(startPanel, false);
            SetActive(hudPanel, true);
            SetActive(resultPanel, false);

            if (truckController != null)
            {
                truckController.enabled = true;
            }

            if (spawnManager != null)
            {
                spawnManager.enabled = true;
            }

            drivingTimeManager?.StartTimer();
        }

        private void EndGame()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            CurrentState = GameState.Result;
            drivingTimeManager?.StopTimer();

            if (truckController != null)
            {
                truckController.enabled = false;
                Rigidbody truckBody = truckController.GetComponent<Rigidbody>();
                if (truckBody != null)
                {
                    truckBody.linearVelocity = Vector3.zero;
                    truckBody.angularVelocity = Vector3.zero;
                }
            }

            if (spawnManager != null)
            {
                spawnManager.enabled = false;
            }

            SetMonsterSimulation(false);
            resultPanelUI?.ShowResults();
            SetActive(resultPanel, true);
        }

        private void RestartGame()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void EnterStartState()
        {
            CurrentState = GameState.Start;
            SetActive(startPanel, true);
            SetActive(hudPanel, false);
            SetActive(resultPanel, false);

            if (truckController != null)
            {
                truckController.enabled = false;
            }

            if (spawnManager != null)
            {
                spawnManager.enabled = false;
            }

            drivingTimeManager?.StopTimer();
        }

        private void SetMonsterSimulation(bool active)
        {
            if (monstersParent == null)
            {
                return;
            }

            MonsterController[] monsters = monstersParent.GetComponentsInChildren<MonsterController>(true);
            foreach (MonsterController monster in monsters)
            {
                monster.enabled = active;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
