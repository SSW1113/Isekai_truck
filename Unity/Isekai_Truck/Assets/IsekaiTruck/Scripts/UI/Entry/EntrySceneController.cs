using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IsekaiTruck.UI.Entry
{
    [DisallowMultipleComponent]
    public sealed class EntrySceneController : MonoBehaviour
    {
        private const string MainSceneName = "Main";

        [SerializeField] private TitleTruckEntrance truckEntrance;
        [SerializeField] private TMP_Text inputGuideText;
        [SerializeField] private Button inputSurface;
        [SerializeField, Min(0.1f)] private float guideBlinkSpeed = 2.4f;

        private bool isLoading;
        private bool isEntranceComplete;

        private void Awake()
        {
            inputGuideText.gameObject.SetActive(false);
            inputSurface.interactable = false;
            truckEntrance.Completed += HandleTruckEntranceCompleted;
            inputSurface.onClick.AddListener(StartGame);
        }

        private void Update()
        {
            AnimateInputGuide();

            Keyboard keyboard = Keyboard.current;
            if (isEntranceComplete && !isLoading && keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                StartGame();
            }
        }

        private void AnimateInputGuide()
        {
            if (!isEntranceComplete || isLoading)
            {
                return;
            }

            Color color = inputGuideText.color;
            color.a = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(Time.unscaledTime * guideBlinkSpeed) + 1f) * 0.5f);
            inputGuideText.color = color;
        }

        private void StartGame()
        {
            if (!isEntranceComplete || isLoading)
            {
                return;
            }

            isLoading = true;
            inputSurface.interactable = false;

            Color color = inputGuideText.color;
            color.a = 0f;
            inputGuideText.color = color;

            SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        }

        private void HandleTruckEntranceCompleted()
        {
            isEntranceComplete = true;
            inputSurface.interactable = true;
            inputGuideText.gameObject.SetActive(true);

            Color color = inputGuideText.color;
            color.a = 1f;
            inputGuideText.color = color;
        }

        private void OnDestroy()
        {
            if (truckEntrance != null)
            {
                truckEntrance.Completed -= HandleTruckEntranceCompleted;
            }

            if (inputSurface != null)
            {
                inputSurface.onClick.RemoveListener(StartGame);
            }
        }

#if UNITY_EDITOR
        public void SetReferences(TitleTruckEntrance targetTruckEntrance, TMP_Text targetInputGuideText, Button targetInputSurface)
        {
            truckEntrance = targetTruckEntrance;
            inputGuideText = targetInputGuideText;
            inputSurface = targetInputSurface;
        }
#endif
    }
}
