using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IsekaiTruck.Audio
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameSfxPlayer : MonoBehaviour
    {
        private const string CollisionClipPath = "Audio/SFX/TruckMonsterCollision";
        private const string LevelUpClipPath = "Audio/SFX/LevelUp";
        private const string UIButtonClipPath = "Audio/SFX/UIButtonClick";

        private static GameSfxPlayer instance;

        private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
        private AudioSource audioSource;
        private AudioClip collisionClip;
        private AudioClip levelUpClip;
        private AudioClip uiButtonClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject playerObject = new GameObject("Game SFX Player");
            instance = playerObject.AddComponent<GameSfxPlayer>();
            DontDestroyOnLoad(playerObject);
        }

        public static void PlayTruckMonsterCollision()
        {
            instance?.PlayOneShot(instance.collisionClip);
        }

        public static void PlayLevelUp()
        {
            instance?.PlayOneShot(instance.levelUpClip);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;

            collisionClip = Resources.Load<AudioClip>(CollisionClipPath);
            levelUpClip = Resources.Load<AudioClip>(LevelUpClipPath);
            uiButtonClip = Resources.Load<AudioClip>(UIButtonClipPath);
            ValidateClips();

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            RegisterSceneButtons();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterSceneButtons();
        }

        private void RegisterSceneButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button button in buttons)
            {
                if (registeredButtons.Add(button))
                {
                    button.onClick.AddListener(PlayUIButtonClick);
                }
            }
        }

        private void PlayUIButtonClick()
        {
            PlayOneShot(uiButtonClip);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void ValidateClips()
        {
            if (collisionClip == null || levelUpClip == null || uiButtonClip == null)
            {
                Debug.LogError("Game SFX clips could not be loaded from Resources/Audio/SFX.", this);
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            foreach (Button button in registeredButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(PlayUIButtonClick);
                }
            }

            registeredButtons.Clear();
            instance = null;
        }
    }
}
