using System;
using IsekaiTruck.Player;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    public sealed class ResultPanelUI : MonoBehaviour
    {
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private Text levelValue;
        [SerializeField] private Text defeatedValue;
        [SerializeField] private Text soulValue;
        [SerializeField] private Button restartButton;

        public event Action RestartRequested;

        public void Configure(
            PlayerProgress progress,
            Text level,
            Text defeated,
            Text soul,
            Button restart)
        {
            playerProgress = progress;
            levelValue = level;
            defeatedValue = defeated;
            soulValue = soul;
            restartButton = restart;
        }

        private void Awake()
        {
            restartButton?.onClick.AddListener(RequestRestart);
        }

        private void OnDestroy()
        {
            restartButton?.onClick.RemoveListener(RequestRestart);
        }

        public void ShowResults()
        {
            if (playerProgress == null)
            {
                return;
            }

            if (levelValue != null)
            {
                levelValue.text = playerProgress.Level.ToString();
            }

            if (defeatedValue != null)
            {
                defeatedValue.text = playerProgress.DefeatedMonsters.ToString();
            }

            if (soulValue != null)
            {
                soulValue.text = playerProgress.CurrentSoul.ToString();
            }
        }

        private void RequestRestart()
        {
            RestartRequested?.Invoke();
        }
    }
}
