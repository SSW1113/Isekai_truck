using TMPro;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class WantedLevelUIPresentation : MonoBehaviour
    {
        [Header("Assembly")]
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform animationRoot;
        [SerializeField] private RectTransform[] leftAssemblyPieces;
        [SerializeField] private RectTransform[] rightAssemblyPieces;
        [SerializeField] private RectTransform[] topAssemblyPieces;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField, Min(0.4f)] private float assemblyDuration = 0.68f;

        [Header("Wanted Stars")]
        [SerializeField] private RectTransform[] starIcons;
        [SerializeField] private CanvasGroup[] starCanvasGroups;
        [SerializeField] private RectTransform stageText;
        [SerializeField] private float starClusterCenter = -38f;
        [SerializeField, Min(18f)] private float starSpacing = 24f;
        [SerializeField, Min(0f)] private float stageGap = 8f;
        [SerializeField, Min(0.1f)] private float starAnimationDuration = 0.34f;

        [Header("Feedback")]
        [SerializeField] private CanvasGroup redBeacon;
        [SerializeField] private CanvasGroup blueBeacon;
        [SerializeField, Min(0.1f)] private float shakeDuration = 0.28f;
        [SerializeField, Min(0f)] private float shakeStrength = 7f;
        [SerializeField, Min(0.08f)] private float beaconInterval = 0.2f;
        [SerializeField, Min(0.1f)] private float beaconAlertDuration = 0.9f;

        private AssemblyGroupState leftGroup;
        private AssemblyGroupState rightGroup;
        private AssemblyGroupState topGroup;
        private Vector2 animationRootRestingPosition;
        private Quaternion animationRootRestingRotation;
        private Vector3 animationRootRestingScale;
        private Vector3 contentRestingScale;
        private int[] starOrder;
        private Vector2[] starStartPositions;
        private Vector2[] starTargetPositions;
        private bool[] newStarFlags;
        private Vector2 stageStartPosition;
        private Vector2 stageTargetPosition;
        private int visibleStarCount;
        private float assemblyElapsed;
        private float starElapsed;
        private float shakeElapsed;
        private float beaconElapsed;
        private float beaconAlertRemaining;
        private bool isInitialized;
        private bool isAssemblyPlaying;
        private bool isStarAnimationPlaying;
        private bool isShakePlaying;
        private bool isContinuousBeaconActive;
        private bool isRedBeaconLit;
        private int assembledLevel;

        public int VisibleStarCount => visibleStarCount;
        public bool IsAssemblyPlaying => isAssemblyPlaying;
        public bool IsContinuousBeaconActive => isContinuousBeaconActive;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            UpdateAssembly(deltaTime);
            UpdateStarAnimation(deltaTime);
            UpdateShake(deltaTime);
            UpdateBeacons(deltaTime);
        }

        public void ShowInitialState(int level)
        {
            EnsureInitialized();
            if (level <= 0)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            ResetMotion();
            SetStarsImmediate(level);
            SetBeaconMode(level, false);
        }

        public void PlayAssembly(int level)
        {
            EnsureInitialized();
            gameObject.SetActive(true);
            ResetMotion();
            SetStarsImmediate(level);
            assembledLevel = level;
            isAssemblyPlaying = true;
            assemblyElapsed = 0f;
            contentGroup.alpha = 0f;
            contentGroup.transform.localScale = contentRestingScale * 0.72f;
            ApplyAssemblyStartPositions();
            SetBeaconMode(0, false);
        }

        public void PlayLevelIncrease(int previousLevel, int level)
        {
            EnsureInitialized();
            gameObject.SetActive(true);
            assembledLevel = level;
            AnimateStars(previousLevel, level);
            StartShake();
            SetBeaconMode(level, true);
        }

        public void Hide()
        {
            EnsureInitialized();
            ResetMotion();
            SetStarsImmediate(0);
            SetBeaconMode(0, false);
            gameObject.SetActive(false);
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            leftGroup = new AssemblyGroupState(leftAssemblyPieces);
            rightGroup = new AssemblyGroupState(rightAssemblyPieces);
            topGroup = new AssemblyGroupState(topAssemblyPieces);
            animationRootRestingPosition = animationRoot.anchoredPosition;
            animationRootRestingRotation = animationRoot.localRotation;
            animationRootRestingScale = animationRoot.localScale;
            contentRestingScale = contentGroup.transform.localScale;

            int starCount = starIcons.Length;
            starOrder = new int[starCount];
            starStartPositions = new Vector2[starCount];
            starTargetPositions = new Vector2[starCount];
            newStarFlags = new bool[starCount];
            isInitialized = true;
        }

        private void ApplyAssemblyStartPositions()
        {
            float viewportWidth = Mathf.Max(viewport.rect.width, Screen.width);
            float viewportHeight = Mathf.Max(viewport.rect.height, Screen.height);
            float horizontalOffset = viewportWidth * 0.5f + 360f;
            float verticalOffset = viewportHeight * 0.45f + 180f;

            leftGroup.ApplyStart(new Vector2(-horizontalOffset, verticalOffset * 0.18f), -12f);
            rightGroup.ApplyStart(new Vector2(horizontalOffset, verticalOffset * 0.18f), 12f);
            topGroup.ApplyStart(new Vector2(0f, verticalOffset), 0f);
        }

        private void UpdateAssembly(float deltaTime)
        {
            if (!isAssemblyPlaying)
            {
                return;
            }

            assemblyElapsed += deltaTime;
            leftGroup.Evaluate(assemblyElapsed, 0f, 0.32f, 0.04f);
            rightGroup.Evaluate(assemblyElapsed, 0.06f, 0.32f, 0.04f);
            topGroup.Evaluate(assemblyElapsed, 0.14f, 0.34f, 0.055f);

            float contentProgress = Mathf.Clamp01((assemblyElapsed - 0.46f) / 0.18f);
            contentGroup.alpha = contentProgress;
            contentGroup.transform.localScale = contentRestingScale * PopScale(contentProgress);

            if (assemblyElapsed < assemblyDuration)
            {
                return;
            }

            isAssemblyPlaying = false;
            assemblyElapsed = 0f;
            RestoreAssemblyPieces();
            contentGroup.alpha = 1f;
            contentGroup.transform.localScale = contentRestingScale;
            StartShake();
            SetBeaconMode(assembledLevel, true);
        }

        private void AnimateStars(int previousLevel, int level)
        {
            int clampedPreviousLevel = Mathf.Clamp(previousLevel, 0, starIcons.Length);
            int clampedLevel = Mathf.Clamp(level, 0, starIcons.Length);
            if (visibleStarCount != clampedPreviousLevel)
            {
                SetStarsImmediate(clampedPreviousLevel);
            }

            for (int i = 0; i < starIcons.Length; i++)
            {
                starStartPositions[i] = starIcons[i].anchoredPosition;
                newStarFlags[i] = false;
            }

            for (int starIndex = clampedPreviousLevel; starIndex < clampedLevel; starIndex++)
            {
                InsertStarAtCenter(starIndex);
                starIcons[starIndex].gameObject.SetActive(true);
                starIcons[starIndex].anchoredPosition = new Vector2(starClusterCenter, 0f);
                starIcons[starIndex].localScale = Vector3.zero;
                starCanvasGroups[starIndex].alpha = 0f;
                starStartPositions[starIndex] = starIcons[starIndex].anchoredPosition;
                newStarFlags[starIndex] = true;
            }

            visibleStarCount = clampedLevel;
            CalculateStarTargets();
            stageStartPosition = stageText.anchoredPosition;
            stageTargetPosition = CalculateStagePosition(visibleStarCount);
            starElapsed = 0f;
            isStarAnimationPlaying = clampedLevel > clampedPreviousLevel;

            if (!isStarAnimationPlaying)
            {
                ApplyStarTargets();
            }
        }

        private void UpdateStarAnimation(float deltaTime)
        {
            if (!isStarAnimationPlaying)
            {
                return;
            }

            starElapsed += deltaTime;
            float progress = Mathf.Clamp01(starElapsed / starAnimationDuration);
            float moveProgress = SmoothStep(progress);

            for (int i = 0; i < visibleStarCount; i++)
            {
                RectTransform star = starIcons[i];
                star.anchoredPosition = Vector2.LerpUnclamped(starStartPositions[i], starTargetPositions[i], moveProgress);
                if (newStarFlags[i])
                {
                    star.localScale = Vector3.one * PopScale(progress);
                    starCanvasGroups[i].alpha = Mathf.Clamp01(progress / 0.28f);
                }
            }

            stageText.anchoredPosition = Vector2.LerpUnclamped(stageStartPosition, stageTargetPosition, moveProgress);

            if (progress < 1f)
            {
                return;
            }

            isStarAnimationPlaying = false;
            starElapsed = 0f;
            ApplyStarTargets();
        }

        private void SetStarsImmediate(int level)
        {
            int clampedLevel = Mathf.Clamp(level, 0, starIcons.Length);
            visibleStarCount = 0;

            for (int i = 0; i < starIcons.Length; i++)
            {
                starIcons[i].gameObject.SetActive(i < clampedLevel);
                starIcons[i].localScale = Vector3.one;
                starCanvasGroups[i].alpha = 1f;
                newStarFlags[i] = false;
            }

            for (int starIndex = 0; starIndex < clampedLevel; starIndex++)
            {
                InsertStarAtCenter(starIndex);
            }

            visibleStarCount = clampedLevel;
            CalculateStarTargets();
            ApplyStarTargets();
            stageText.gameObject.SetActive(clampedLevel > 0);
        }

        private void InsertStarAtCenter(int starIndex)
        {
            int insertionIndex = (visibleStarCount + 1) / 2;
            for (int i = visibleStarCount; i > insertionIndex; i--)
            {
                starOrder[i] = starOrder[i - 1];
            }

            starOrder[insertionIndex] = starIndex;
            visibleStarCount++;
        }

        private void CalculateStarTargets()
        {
            for (int slotIndex = 0; slotIndex < visibleStarCount; slotIndex++)
            {
                int starIndex = starOrder[slotIndex];
                float offset = (slotIndex - (visibleStarCount - 1) * 0.5f) * starSpacing;
                starTargetPositions[starIndex] = new Vector2(starClusterCenter + offset, 0f);
            }
        }

        private void ApplyStarTargets()
        {
            for (int i = 0; i < visibleStarCount; i++)
            {
                starIcons[i].anchoredPosition = starTargetPositions[i];
                starIcons[i].localScale = Vector3.one;
                starCanvasGroups[i].alpha = 1f;
            }

            stageText.anchoredPosition = CalculateStagePosition(visibleStarCount);
        }

        private Vector2 CalculateStagePosition(int starCount)
        {
            float rightmostStarCenter = starClusterCenter + Mathf.Max(0, starCount - 1) * starSpacing * 0.5f;
            float starHalfWidth = starIcons.Length > 0 ? starIcons[0].sizeDelta.x * 0.5f : 0f;
            float labelHalfWidth = stageText.sizeDelta.x * 0.5f;
            return new Vector2(rightmostStarCenter + starHalfWidth + stageGap + labelHalfWidth, 0f);
        }

        private void StartShake()
        {
            shakeElapsed = 0f;
            isShakePlaying = true;
        }

        private void UpdateShake(float deltaTime)
        {
            if (!isShakePlaying)
            {
                return;
            }

            shakeElapsed += deltaTime;
            float progress = Mathf.Clamp01(shakeElapsed / shakeDuration);
            float damping = 1f - progress;
            float wave = Mathf.Sin(progress * Mathf.PI * 12f);
            float secondaryWave = Mathf.Sin(progress * Mathf.PI * 17f);
            animationRoot.anchoredPosition = animationRootRestingPosition + new Vector2(
                wave * shakeStrength * damping,
                secondaryWave * shakeStrength * 0.24f * damping
            );
            animationRoot.localRotation = animationRootRestingRotation * Quaternion.Euler(0f, 0f, wave * damping);

            if (progress < 1f)
            {
                return;
            }

            isShakePlaying = false;
            shakeElapsed = 0f;
            RestoreAnimationRoot();
        }

        private void SetBeaconMode(int level, bool playAlert)
        {
            isContinuousBeaconActive = level >= 5;
            beaconAlertRemaining = playAlert && level > 0 && !isContinuousBeaconActive ? beaconAlertDuration : 0f;
            beaconElapsed = 0f;
            isRedBeaconLit = true;

            if (isContinuousBeaconActive || beaconAlertRemaining > 0f)
            {
                ApplyBeaconFlash();
            }
            else
            {
                ApplyBeaconRestingState(level > 0);
            }
        }

        private void UpdateBeacons(float deltaTime)
        {
            bool shouldFlash = isContinuousBeaconActive || beaconAlertRemaining > 0f;
            if (!shouldFlash)
            {
                return;
            }

            if (!isContinuousBeaconActive)
            {
                beaconAlertRemaining -= deltaTime;
            }

            beaconElapsed += deltaTime;
            if (beaconElapsed >= beaconInterval)
            {
                beaconElapsed = Mathf.Repeat(beaconElapsed, beaconInterval);
                isRedBeaconLit = !isRedBeaconLit;
                ApplyBeaconFlash();
            }

            if (!isContinuousBeaconActive && beaconAlertRemaining <= 0f)
            {
                beaconAlertRemaining = 0f;
                beaconElapsed = 0f;
                ApplyBeaconRestingState(true);
            }
        }

        private void ApplyBeaconFlash()
        {
            redBeacon.alpha = isRedBeaconLit ? 1f : 0.22f;
            blueBeacon.alpha = isRedBeaconLit ? 0.22f : 1f;
        }

        private void ApplyBeaconRestingState(bool hasWantedLevel)
        {
            redBeacon.alpha = hasWantedLevel ? 0.7f : 0.22f;
            blueBeacon.alpha = hasWantedLevel ? 0.5f : 0.22f;
        }

        private void ResetMotion()
        {
            isAssemblyPlaying = false;
            isStarAnimationPlaying = false;
            isShakePlaying = false;
            assemblyElapsed = 0f;
            starElapsed = 0f;
            shakeElapsed = 0f;
            RestoreAssemblyPieces();
            RestoreAnimationRoot();
            contentGroup.alpha = 1f;
            contentGroup.transform.localScale = contentRestingScale;
        }

        private void RestoreAssemblyPieces()
        {
            leftGroup.Restore();
            rightGroup.Restore();
            topGroup.Restore();
        }

        private void RestoreAnimationRoot()
        {
            animationRoot.anchoredPosition = animationRootRestingPosition;
            animationRoot.localRotation = animationRootRestingRotation;
            animationRoot.localScale = animationRootRestingScale;
        }

        private void OnDisable()
        {
            if (!isInitialized)
            {
                return;
            }

            ResetMotion();
        }

        private static float SmoothStep(float progress)
        {
            return progress * progress * (3f - 2f * progress);
        }

        private static float BackOut(float progress)
        {
            const float Overshoot = 1.7f;
            float shifted = progress - 1f;
            return 1f + (Overshoot + 1f) * shifted * shifted * shifted + Overshoot * shifted * shifted;
        }

        private static float PopScale(float progress)
        {
            if (progress <= 0f)
            {
                return 0f;
            }

            if (progress < 0.68f)
            {
                return Mathf.LerpUnclamped(0f, 1.25f, EaseOutCubic(progress / 0.68f));
            }

            return Mathf.Lerp(1.25f, 1f, SmoothStep((progress - 0.68f) / 0.32f));
        }

        private static float EaseOutCubic(float progress)
        {
            float remaining = 1f - progress;
            return 1f - remaining * remaining * remaining;
        }

#if UNITY_EDITOR
        public void SetReferences(
            RectTransform targetViewport,
            RectTransform targetAnimationRoot,
            RectTransform[] targetLeftAssemblyPieces,
            RectTransform[] targetRightAssemblyPieces,
            RectTransform[] targetTopAssemblyPieces,
            CanvasGroup targetContentGroup,
            RectTransform[] targetStarIcons,
            CanvasGroup[] targetStarCanvasGroups,
            RectTransform targetStageText,
            CanvasGroup targetRedBeacon,
            CanvasGroup targetBlueBeacon
        )
        {
            viewport = targetViewport;
            animationRoot = targetAnimationRoot;
            leftAssemblyPieces = targetLeftAssemblyPieces;
            rightAssemblyPieces = targetRightAssemblyPieces;
            topAssemblyPieces = targetTopAssemblyPieces;
            contentGroup = targetContentGroup;
            starIcons = targetStarIcons;
            starCanvasGroups = targetStarCanvasGroups;
            stageText = targetStageText;
            redBeacon = targetRedBeacon;
            blueBeacon = targetBlueBeacon;
        }

        public void CompleteAnimationsImmediately()
        {
            EnsureInitialized();
            if (isAssemblyPlaying)
            {
                isAssemblyPlaying = false;
                RestoreAssemblyPieces();
                contentGroup.alpha = 1f;
                contentGroup.transform.localScale = contentRestingScale;
                SetBeaconMode(assembledLevel, true);
            }

            isStarAnimationPlaying = false;
            ApplyStarTargets();
            isShakePlaying = false;
            RestoreAnimationRoot();
        }
#endif

        private sealed class AssemblyGroupState
        {
            private readonly RectTransform[] pieces;
            private readonly Vector2[] restingPositions;
            private readonly Quaternion[] restingRotations;
            private readonly Vector2[] startPositions;
            private readonly Quaternion[] startRotations;

            public AssemblyGroupState(RectTransform[] targetPieces)
            {
                pieces = targetPieces;
                restingPositions = new Vector2[pieces.Length];
                restingRotations = new Quaternion[pieces.Length];
                startPositions = new Vector2[pieces.Length];
                startRotations = new Quaternion[pieces.Length];

                for (int i = 0; i < pieces.Length; i++)
                {
                    restingPositions[i] = pieces[i].anchoredPosition;
                    restingRotations[i] = pieces[i].localRotation;
                }
            }

            public void ApplyStart(Vector2 offset, float rotationOffset)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    startPositions[i] = restingPositions[i] + offset;
                    startRotations[i] = restingRotations[i] * Quaternion.Euler(0f, 0f, rotationOffset);
                    pieces[i].anchoredPosition = startPositions[i];
                    pieces[i].localRotation = startRotations[i];
                }
            }

            public void Evaluate(float elapsed, float delay, float duration, float stagger)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    float progress = Mathf.Clamp01((elapsed - delay - stagger * i) / duration);
                    float eased = BackOut(progress);
                    pieces[i].anchoredPosition = Vector2.LerpUnclamped(startPositions[i], restingPositions[i], eased);
                    pieces[i].localRotation = Quaternion.SlerpUnclamped(startRotations[i], restingRotations[i], eased);
                }
            }

            public void Restore()
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    pieces[i].anchoredPosition = restingPositions[i];
                    pieces[i].localRotation = restingRotations[i];
                }
            }
        }
    }
}
