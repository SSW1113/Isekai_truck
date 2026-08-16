using IsekaiTruck.Truck;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class TruckHealthUIController : MonoBehaviour
    {
        [SerializeField] private Text healthText;

        private TruckHealthController healthController;

        public void Initialize(TruckHealthController health)
        {
            healthController = health;
            healthController.StateChanged += HandleStateChanged;
            Refresh(healthController.GetState());
        }

        private void HandleStateChanged(TruckHealthSnapshot state)
        {
            Refresh(state);
        }

        private void Refresh(TruckHealthSnapshot state)
        {
            healthText.text = $"체력 {state.CurrentHealth} / {state.MaxHealth}";
        }

        private void OnDestroy()
        {
            if (healthController != null)
            {
                healthController.StateChanged -= HandleStateChanged;
            }
        }

#if UNITY_EDITOR
        public void SetReferences(Text targetHealthText)
        {
            healthText = targetHealthText;
        }
#endif
    }
}
