using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Enemies;
using IsekaiTruck.Monsters;
using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Blessings
{
    [DisallowMultipleComponent]
    public sealed class BlessingEffectSystem : MonoBehaviour
    {
        private BlessingLoadoutSystem loadoutSystem;
        private TruckController truckController;
        private CameraController cameraController;
        private MonsterManager monsterManager;
        private EnemyManager enemyManager;
        private Transform truck;
        private float[] activeRemaining;
        private float[] periodicRemaining;
        private string[] activeIds;
        private string[] trackedIds;

        public float ExperienceMultiplier { get; private set; } = 1f;
        public bool IsWorldTimeStopped { get; private set; }

        public event Action StateChanged;

        public void Initialize(BlessingLoadoutSystem loadout, TruckController truckControllerTarget, CameraController cameraControllerTarget, MonsterManager monsterManagerTarget)
        {
            Initialize(loadout, truckControllerTarget, cameraControllerTarget, monsterManagerTarget, null);
        }

        public void Initialize(BlessingLoadoutSystem loadout, TruckController truckControllerTarget, CameraController cameraControllerTarget, MonsterManager monsterManagerTarget, EnemyManager enemyManagerTarget)
        {
            loadoutSystem = loadout;
            truckController = truckControllerTarget;
            cameraController = cameraControllerTarget;
            monsterManager = monsterManagerTarget;
            enemyManager = enemyManagerTarget;
            truck = truckController.transform;
            activeRemaining = new float[loadoutSystem.SlotCount];
            periodicRemaining = new float[loadoutSystem.SlotCount];
            activeIds = new string[loadoutSystem.SlotCount];
            trackedIds = new string[loadoutSystem.SlotCount];
            loadoutSystem.StateChanged += HandleLoadoutChanged;
            HandleLoadoutChanged();
        }

        public void UpdateEffects(float deltaTime)
        {
            bool stateChanged = false;
            float safeDeltaTime = Mathf.Max(0f, deltaTime);

            for (int i = 0; i < loadoutSystem.SlotCount; i++)
            {
                BlessingDefinition definition = loadoutSystem.GetEquipped(i);
                if (activeRemaining[i] > 0f)
                {
                    activeRemaining[i] = Mathf.Max(0f, activeRemaining[i] - safeDeltaTime);
                    if (activeRemaining[i] <= 0f)
                    {
                        activeIds[i] = null;
                        stateChanged = true;
                    }
                }

                if (definition == null || definition.ActivationType != BlessingActivationType.Passive || definition.EffectType != BlessingEffectType.PeriodicStun)
                {
                    continue;
                }

                periodicRemaining[i] -= safeDeltaTime;
                if (periodicRemaining[i] <= 0f)
                {
                    monsterManager.StunNearest(truck.position, definition.Radius, definition.Duration);
                    periodicRemaining[i] = Mathf.Max(definition.Interval, 0.01f);
                }
            }

            ApplyModifiers();
            if (stateChanged)
            {
                StateChanged?.Invoke();
            }
        }

        public bool TryActivate(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= loadoutSystem.SlotCount || activeRemaining[slotIndex] > 0f)
            {
                return false;
            }

            BlessingDefinition definition = loadoutSystem.GetEquipped(slotIndex);
            if (definition == null || definition.ActivationType != BlessingActivationType.Active || definition.Duration <= 0f)
            {
                return false;
            }

            activeIds[slotIndex] = definition.Id;
            activeRemaining[slotIndex] = definition.Duration;
            ApplyModifiers();
            StateChanged?.Invoke();
            return true;
        }

        public float GetRemainingDuration(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < activeRemaining.Length ? activeRemaining[slotIndex] : 0f;
        }

        public bool CanActivate(int slotIndex)
        {
            BlessingDefinition definition = loadoutSystem.GetEquipped(slotIndex);
            return definition != null && definition.ActivationType == BlessingActivationType.Active && activeRemaining[slotIndex] <= 0f;
        }

        private void HandleLoadoutChanged()
        {
            for (int i = 0; i < loadoutSystem.SlotCount; i++)
            {
                BlessingDefinition definition = loadoutSystem.GetEquipped(i);
                string equippedId = definition?.Id;
                if (activeIds[i] != equippedId)
                {
                    activeIds[i] = null;
                    activeRemaining[i] = 0f;
                }

                if (trackedIds[i] != equippedId)
                {
                    trackedIds[i] = equippedId;
                    periodicRemaining[i] = definition != null && definition.EffectType == BlessingEffectType.PeriodicStun
                        ? definition.Interval
                        : 0f;
                }
            }

            ApplyModifiers();
            StateChanged?.Invoke();
        }

        private void ApplyModifiers()
        {
            float experienceMultiplier = 1f;
            float truckSpeedMultiplier = 1f;
            float truckSizeMultiplier = 1f;
            float viewMultiplier = 1f;
            float monsterSpeedMultiplier = 1f;
            float monsterSlowRadius = 0f;
            bool isTimeStopped = false;

            for (int i = 0; i < loadoutSystem.SlotCount; i++)
            {
                BlessingDefinition definition = loadoutSystem.GetEquipped(i);
                if (definition == null)
                {
                    continue;
                }

                bool isApplied = definition.ActivationType == BlessingActivationType.Passive || activeRemaining[i] > 0f && activeIds[i] == definition.Id;
                if (!isApplied)
                {
                    continue;
                }

                switch (definition.EffectType)
                {
                    case BlessingEffectType.MonsterSlow:
                        monsterSpeedMultiplier *= definition.EffectValue;
                        monsterSlowRadius = Mathf.Max(monsterSlowRadius, definition.Radius);
                        break;
                    case BlessingEffectType.VisionBoost:
                        viewMultiplier *= definition.EffectValue;
                        break;
                    case BlessingEffectType.ExperienceGain:
                        experienceMultiplier *= definition.EffectValue;
                        break;
                    case BlessingEffectType.TruckBoost:
                        truckSpeedMultiplier *= definition.EffectValue;
                        truckSizeMultiplier *= definition.EffectValue;
                        break;
                    case BlessingEffectType.TruckSpeed:
                        truckSpeedMultiplier *= definition.EffectValue;
                        break;
                    case BlessingEffectType.TruckSize:
                        truckSizeMultiplier *= definition.EffectValue;
                        break;
                    case BlessingEffectType.TimeStop:
                        isTimeStopped = true;
                        break;
                }
            }

            ExperienceMultiplier = experienceMultiplier;
            IsWorldTimeStopped = isTimeStopped;
            truckController.SetBlessingMultipliers(truckSpeedMultiplier, truckSizeMultiplier);
            cameraController.SetBlessingViewMultiplier(viewMultiplier);
            monsterManager.SetAreaSpeedModifier(monsterSlowRadius, monsterSpeedMultiplier);
            monsterManager.SetWorldPaused(isTimeStopped);
            enemyManager?.SetWorldPaused(isTimeStopped);
        }

        private void OnDestroy()
        {
            if (loadoutSystem != null)
            {
                loadoutSystem.StateChanged -= HandleLoadoutChanged;
            }
        }
    }
}
