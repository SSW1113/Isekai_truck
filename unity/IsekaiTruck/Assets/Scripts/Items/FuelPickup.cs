using IsekaiTruck.Gameplay;
using UnityEngine;

namespace IsekaiTruck.Items
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class FuelPickup : MonoBehaviour
    {
        [SerializeField] private Transform truck;
        [SerializeField] private DrivingTimeManager drivingTimeManager;
        [SerializeField, Min(0f)] private float fuelTimeBonus = 10f;

        private bool collected;

        public void Configure(Transform truckTarget, DrivingTimeManager timer)
        {
            truck = truckTarget;
            drivingTimeManager = timer;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || truck == null || drivingTimeManager == null)
            {
                return;
            }

            Rigidbody otherBody = other.attachedRigidbody;
            bool touchedTruck = other.transform == truck
                || other.transform.IsChildOf(truck)
                || (otherBody != null && otherBody.transform == truck);

            if (!touchedTruck)
            {
                return;
            }

            collected = true;
            drivingTimeManager.AddTime(fuelTimeBonus);
            Destroy(gameObject);
        }
    }
}
