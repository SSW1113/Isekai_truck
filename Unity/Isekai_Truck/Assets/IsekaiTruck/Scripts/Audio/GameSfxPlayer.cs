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
        private const string TruckDamageClipPath = "Audio/SFX/TruckDamage";
        private const string LevelUpClipPath = "Audio/SFX/LevelUp";
        private const string UIButtonClipPath = "Audio/SFX/UIButtonClick";
        private const string FlyerClipPath = "Audio/SFX/FlyerPaperBalanced";
        private const string RebirthClipPath = "Audio/SFX/RebirthBalanced";
        private const string NinjaSubstitutionClipPath = "Audio/SFX/NinjaSubstitutionBalanced";
        private const string WizardTeleportClipPath = "Audio/SFX/WizardTeleport";
        private const string JeonWoochiSpellClipPath = "Audio/SFX/JeonWoochiSpellBalanced";
        private static readonly string[] SamuraiChargeClipPaths =
        {
            "Audio/SFX/SamuraiCharge1",
            "Audio/SFX/SamuraiCharge2",
            "Audio/SFX/SamuraiCharge3",
            "Audio/SFX/SamuraiCharge4"
        };
        private static readonly string[] WantedLevelClipPaths =
        {
            "Audio/SFX/WantedLevel01",
            "Audio/SFX/WantedLevel02",
            "Audio/SFX/WantedLevel03",
            "Audio/SFX/WantedLevel04",
            "Audio/SFX/WantedLevel05",
            "Audio/SFX/WantedLevel06",
            "Audio/SFX/WantedLevel07",
            "Audio/SFX/WantedLevel08",
            "Audio/SFX/WantedLevel09",
            "Audio/SFX/WantedLevel10"
        };
        private const string BgmClipPath = "Audio/BGM/MainBgm";
        private const float BgmVolume = 0.2f;
        private const float WizardTeleportVolume = 0.45f;
        private const float JeonWoochiSpellVolume = 0.65f;

        private static GameSfxPlayer instance;

        private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
        private AudioSource audioSource;
        private AudioSource bgmAudioSource;
        private AudioClip collisionClip;
        private AudioClip truckDamageClip;
        private AudioClip levelUpClip;
        private AudioClip uiButtonClip;
        private AudioClip flyerClip;
        private AudioClip rebirthClip;
        private AudioClip ninjaSubstitutionClip;
        private AudioClip wizardTeleportClip;
        private AudioClip jeonWoochiSpellClip;
        private AudioClip[] samuraiChargeClips;
        private AudioClip[] wantedLevelClips;
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

        public static void PlayTruckDamage()
        {
            instance?.PlayOneShot(instance.truckDamageClip);
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
            instance?.PlayOneShot(instance.wizardTeleportClip, WizardTeleportVolume);
        }

        public static void PlayJeonWoochiSpell()
        {
            instance?.PlayOneShot(instance.jeonWoochiSpellClip, JeonWoochiSpellVolume);
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

        public static void PlayWantedLevelUp(int level)
        {
            if (instance == null || instance.wantedLevelClips == null || level <= 0 ||
                level > instance.wantedLevelClips.Length)
            {
                return;
            }

            instance.PlayOneShot(instance.wantedLevelClips[level - 1]);
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
            truckDamageClip = Resources.Load<AudioClip>(TruckDamageClipPath);
            levelUpClip = Resources.Load<AudioClip>(LevelUpClipPath);
            uiButtonClip = Resources.Load<AudioClip>(UIButtonClipPath);
            flyerClip = Resources.Load<AudioClip>(FlyerClipPath);
            rebirthClip = Resources.Load<AudioClip>(RebirthClipPath);
            ninjaSubstitutionClip = Resources.Load<AudioClip>(NinjaSubstitutionClipPath);
            wizardTeleportClip = Resources.Load<AudioClip>(WizardTeleportClipPath);
            jeonWoochiSpellClip = Resources.Load<AudioClip>(JeonWoochiSpellClipPath);
            samuraiChargeClips = new AudioClip[SamuraiChargeClipPaths.Length];
            for (int i = 0; i < SamuraiChargeClipPaths.Length; i++)
            {
                samuraiChargeClips[i] = Resources.Load<AudioClip>(SamuraiChargeClipPaths[i]);
            }
            wantedLevelClips = new AudioClip[WantedLevelClipPaths.Length];
            for (int i = 0; i < WantedLevelClipPaths.Length; i++)
            {
                wantedLevelClips[i] = Resources.Load<AudioClip>(WantedLevelClipPaths[i]);
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
            PlayOneShot(clip, 1f);
        }

        private void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
            }
        }

        private void ValidateClips()
        {
            if (collisionClip == null || truckDamageClip == null || levelUpClip == null || uiButtonClip == null || flyerClip == null ||
                rebirthClip == null || ninjaSubstitutionClip == null || wizardTeleportClip == null ||
                jeonWoochiSpellClip == null || bgmClip == null ||
                samuraiChargeClips == null || System.Array.Exists(samuraiChargeClips, clip => clip == null) ||
                wantedLevelClips == null || System.Array.Exists(wantedLevelClips, clip => clip == null))
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
