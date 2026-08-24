using IsekaiTruck.Wanted;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class WantedLevelUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image bannerFace;
        [SerializeField] private WantedLevelUIPresentation presentation;
        [SerializeField] private Color activeBannerColor = new Color32(0xE6, 0x5C, 0x45, 0xFF);

        private WantedLevelSystem wantedLevelSystem;
        private int renderedLevel = -1;

        public void Initialize(WantedLevelSystem wanted)
        {
            if (wantedLevelSystem != null)
            {
                wantedLevelSystem.StateChanged -= HandleStateChanged;
            }

            wantedLevelSystem = wanted;
            wantedLevelSystem.StateChanged += HandleStateChanged;

            WantedLevelSnapshot state = wantedLevelSystem.GetState();
            renderedLevel = state.Level;
            ApplyText(state.Level);
            presentation.ShowInitialState(state.Level);
        }

        private void HandleStateChanged(WantedLevelSnapshot state)
        {
            int previousLevel = renderedLevel;
            if (state.Level == previousLevel)
            {
                return;
            }

            renderedLevel = state.Level;
            ApplyText(state.Level);

            if (state.Level <= 0)
            {
                presentation.Hide();
                return;
            }

            if (previousLevel <= 0)
            {
                presentation.PlayAssembly(state.Level);
                return;
            }

            if (state.Level > previousLevel)
            {
                presentation.PlayLevelIncrease(previousLevel, state.Level);
                return;
            }

            presentation.ShowInitialState(state.Level);
        }

        private void ApplyText(int level)
        {
            statusText.text = "비상! 지명수배";
            levelText.text = $"LV.{level}";
            bannerFace.color = activeBannerColor;
        }

        private void OnDestroy()
        {
            if (wantedLevelSystem != null)
            {
                wantedLevelSystem.StateChanged -= HandleStateChanged;
            }
        }

#if UNITY_EDITOR
        public void SetReferences(
            TMP_Text targetStatusText,
            TMP_Text targetLevelText,
            Image targetBannerFace,
            WantedLevelUIPresentation targetPresentation
        )
        {
            statusText = targetStatusText;
            levelText = targetLevelText;
            bannerFace = targetBannerFace;
            presentation = targetPresentation;
        }
#endif
    }
}
