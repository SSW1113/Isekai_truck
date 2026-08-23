using System.Text;
using IsekaiTruck.Truck;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class TruckHealthUIController : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private UIFeedbackEffect feedbackEffect;

        private TruckHealthController healthController;
        private readonly StringBuilder healthTextBuilder = new StringBuilder(96);
        private int renderedHealth = int.MinValue;

        public void Initialize(TruckHealthController health)
        {
            healthController = health;
            healthController.StateChanged += HandleStateChanged;
            Refresh(healthController.GetState(), false);
        }

        private void HandleStateChanged(TruckHealthSnapshot state)
        {
            Refresh(state, renderedHealth != state.CurrentHealth);
        }

        private void Refresh(TruckHealthSnapshot state, bool animateChange)
        {
            healthTextBuilder.Clear();
            healthTextBuilder.Append("체력  ");

            for (int i = 0; i < state.MaxHealth; i++)
            {
                if (i > 0)
                {
                    healthTextBuilder.Append(' ');
                }

                healthTextBuilder.Append(i < state.CurrentHealth
                    ? "<color=#E990B8>♥</color>"
                    : "<color=#B9A4AF>♡</color>");
            }

            healthText.text = healthTextBuilder.ToString();
            renderedHealth = state.CurrentHealth;
            if (animateChange)
            {
                feedbackEffect?.Play();
            }
        }

        private void OnDestroy()
        {
            if (healthController != null)
            {
                healthController.StateChanged -= HandleStateChanged;
            }
        }

#if UNITY_EDITOR
        public void SetReferences(Text targetHealthText, UIFeedbackEffect targetFeedbackEffect = null)
        {
            healthText = targetHealthText;
            feedbackEffect = targetFeedbackEffect;
        }
#endif
    }
}
