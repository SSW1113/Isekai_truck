using System.Collections.Generic;
using IsekaiTruck.Monsters;
using UnityEngine;

namespace IsekaiTruck.Truck
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TruckController))]
    public sealed class TruckStickySlowController : MonoBehaviour
    {
        private readonly List<StickySlowZone> activeZones = new List<StickySlowZone>();
        private TruckController truckController;
        private bool hasAppliedSpeedMultiplier;

        public int ActiveZoneCount => activeZones.Count;

        private void Awake()
        {
            truckController = GetComponent<TruckController>();
        }

        private void Update()
        {
            UpdateSlowState();
        }

        private void OnDisable()
        {
            if (hasAppliedSpeedMultiplier)
            {
                ApplySpeedMultiplier(1f);
            }
        }

        internal void RegisterZone(StickySlowZone zone)
        {
            if (zone != null && !activeZones.Contains(zone))
            {
                activeZones.Add(zone);
            }
        }

        internal void UnregisterZone(StickySlowZone zone)
        {
            activeZones.Remove(zone);
        }

        public void UpdateSlowState()
        {
            float speedMultiplier = 1f;
            Vector3 truckPosition = transform.position;

            for (int zoneIndex = activeZones.Count - 1; zoneIndex >= 0; zoneIndex--)
            {
                StickySlowZone zone = activeZones[zoneIndex];
                if (zone == null)
                {
                    activeZones.RemoveAt(zoneIndex);
                    continue;
                }

                Vector3 difference = zone.transform.position - truckPosition;
                difference.y = 0f;
                float radius = zone.Radius;
                if (difference.sqrMagnitude <= radius * radius)
                {
                    speedMultiplier = Mathf.Min(speedMultiplier, zone.SpeedMultiplier);
                }
            }

            ApplySpeedMultiplier(speedMultiplier);
        }

        private void ApplySpeedMultiplier(float speedMultiplier)
        {
            if (truckController == null)
            {
                truckController = GetComponent<TruckController>();
            }

            if (truckController == null)
            {
                return;
            }

            truckController.SetEnvironmentSpeedMultiplier(speedMultiplier);
            hasAppliedSpeedMultiplier = true;
        }
    }
}
