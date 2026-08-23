using System;
using System.Collections.Generic;
using IsekaiTruck.Blessings;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class BlessingSelectionUI : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private BlessingCardView[] cards;

        public bool IsOpen => overlay != null && overlay.activeSelf;

        public event Action<int> BlessingSelected;

        private void Awake()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Selected += HandleCardSelected;
            }

            overlay.SetActive(false);
        }

        public void Show(IReadOnlyList<BlessingDefinition> candidates, BlessingSystem blessingSystem)
        {
            int cardCount = Mathf.Min(cards.Length, candidates.Count);
            for (int i = 0; i < cards.Length; i++)
            {
                bool hasCandidate = i < cardCount;
                cards[i].gameObject.SetActive(hasCandidate);
                if (hasCandidate)
                {
                    BlessingDefinition blessing = candidates[i];
                    cards[i].SetData(blessing, i, blessingSystem.GetOwnedCount(blessing.Id));
                }
            }

            overlay.SetActive(true);
        }

        public void Hide()
        {
            overlay.SetActive(false);
        }

#if UNITY_EDITOR
        public void SetReferences(GameObject targetOverlay, BlessingCardView[] targetCards)
        {
            overlay = targetOverlay;
            cards = targetCards;
        }
#endif

        private void HandleCardSelected(int candidateIndex)
        {
            BlessingSelected?.Invoke(candidateIndex);
        }

        private void OnDestroy()
        {
            if (cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                {
                    cards[i].Selected -= HandleCardSelected;
                }
            }
        }
    }
}
