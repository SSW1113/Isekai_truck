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
        private const string FlyerClipPath = "Audio/SFX/FlyerPaperBalanced";
        private const string RebirthClipPath = "Audio/SFX/RebirthBalanced";
        private const string NinjaSubstitutionClipPath = "Audio/SFX/NinjaSubstitutionBalanced";
        private const string WizardTeleportClipPath = "Audio/SFX/WizardTeleport";
        private static readonly string[] SamuraiChargeClipPaths =
        {
            "Audio/SFX/SamuraiCharge1",
            "Audio/SFX/SamuraiCharge2",
            "Audio/SFX/SamuraiCharge3",
            "Audio/SFX/SamuraiCharge4"
        };
        private const string BgmClipPath = "Audio/BGM/MainBgm";
        private const float BgmVolume = 0.2f;

        private static GameSfxPlayer instance;

        private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
        private AudioSource audioSource;
        private AudioSource bgmAudioSource;
        private AudioClip collisionClip;
        private AudioClip levelUpClip;
        private AudioClip uiButtonClip;
        private AudioClip flyerClip;
        private AudioClip rebirthClip;
        private AudioClip ninjaSubstitutionClip;
        private AudioClip wizardTeleportClip;
        private AudioClip[] samuraiChargeClips;
        private AudioClip bgmClip;

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

        public static void PlayFlyerOverlay()
        {
            instance?.PlayOneShot(instance.flyerClip);
        }

        public static void PlayRebirth()
        {
            instance?.PlayOneShot(instance.rebirthClip);
        }

        public static void PlayNinjaSubstitution()
        {
            instance?.PlayOneShot(instance.ninjaSubstitutionClip);
        }

        public static void PlayWizardTeleport()
        {
            instance?.PlayOneShot(instance.wizardTeleportClip);
        }

        public static void PlayRandomSamuraiCharge()
        {
            if (instance == null || instance.samuraiChargeClips == null || instance.samuraiChargeClips.Length == 0)
            {
                return;
            }

            int clipIndex = Random.Range(0, instance.samuraiChargeClips.Length);
            instance.PlayOneShot(instance.samuraiChargeClips[clipIndex]);
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

            bgmAudioSource = gameObject.AddComponent<AudioSource>();
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.spatialBlend = 0f;
            bgmAudioSource.volume = BgmVolume;

            collisionClip = Resources.Load<AudioClip>(CollisionClipPath);
            levelUpClip = Resources.Load<AudioClip>(LevelUpClipPath);
            uiButtonClip = Resources.Load<AudioClip>(UIButtonClipPath);
            flyerClip = Resources.Load<AudioClip>(FlyerClipPath);
            rebirthClip = Resources.Load<AudioClip>(RebirthClipPath);
            ninjaSubstitutionClip = Resources.Load<AudioClip>(NinjaSubstitutionClipPath);
            wizardTeleportClip = Resources.Load<AudioClip>(WizardTeleportClipPath);
            samuraiChargeClips = new AudioClip[SamuraiChargeClipPaths.Length];
            for (int i = 0; i < SamuraiChargeClipPaths.Length; i++)
            {
                samuraiChargeClips[i] = Resources.Load<AudioClip>(SamuraiChargeClipPaths[i]);
            }
            bgmClip = Resources.Load<AudioClip>(BgmClipPath);
            ValidateClips();

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (bgmClip != null)
            {
                bgmAudioSource.clip = bgmClip;
                bgmAudioSource.Play();
            }

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
            if (collisionClip == null || levelUpClip == null || uiButtonClip == null || flyerClip == null ||
                rebirthClip == null || ninjaSubstitutionClip == null || wizardTeleportClip == null || bgmClip == null ||
                samuraiChargeClips == null || System.Array.Exists(samuraiChargeClips, clip => clip == null))
            {
                Debug.LogError("Game audio clips could not be loaded from Resources/Audio.", this);
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
