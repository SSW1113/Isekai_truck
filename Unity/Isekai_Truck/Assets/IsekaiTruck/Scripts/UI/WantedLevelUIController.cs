using IsekaiTruck.Wanted;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class WantedLevelUIController : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private RectTransform[] starFillMasks;
        [SerializeField, Min(1f)] private float starWidth = 42f;

        private WantedLevelSystem wantedLevelSystem;

        public void Initialize(WantedLevelSystem wanted)
        {
            wantedLevelSystem = wanted;
            wantedLevelSystem.StateChanged += HandleStateChanged;
            Refresh(wantedLevelSystem.GetState());
        }

        private void Refresh(WantedLevelSnapshot state)
        {
            levelText.text = $"지명수배 Lv.{state.Level}";

            for (int i = 0; i < starFillMasks.Length; i++)
            {
                float fill = Mathf.Clamp(state.Level - i * 2, 0, 2) * 0.5f;
                Vector2 size = starFillMasks[i].sizeDelta;
                size.x = starWidth * fill;
                starFillMasks[i].sizeDelta = size;
            }
        }

        private void HandleStateChanged(WantedLevelSnapshot state)
        {
            Refresh(state);
        }

        private void OnDestroy()
        {
            if (wantedLevelSystem != null) wantedLevelSystem.StateChanged -= HandleStateChanged;
        }

#if UNITY_EDITOR
        public void SetReferences(Text targetLevelText, RectTransform[] targetStarFillMasks, float targetStarWidth)
        {
            levelText = targetLevelText;
            starFillMasks = targetStarFillMasks;
            starWidth = targetStarWidth;
        }
#endif
    }
}
