using System;
using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Truck
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TruckDamageFlash))]
    public sealed class TruckHealthController : MonoBehaviour
    {
        private GameConfig.TruckSettings settings;
        private TruckDamageFlash damageFlash;
        private int currentHealth;
        private float invulnerabilityRemaining;
        private bool isDefeated;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => settings?.MaxHealth ?? 0;
        public bool IsInvulnerable => invulnerabilityRemaining > 0f;
        public bool IsDefeated => isDefeated;

        public event Action<TruckHealthSnapshot> StateChanged;
        public event Action<TruckDamageResult> DamageTaken;
        public event Action Defeated;

        public void Initialize(GameConfig gameConfig, TruckDamageFlash flash)
        {
            settings = gameConfig.Truck;
            damageFlash = flash;
            damageFlash.Initialize(settings.DamageFlashInterval);
            currentHealth = settings.MaxHealth;
            invulnerabilityRemaining = 0f;
            isDefeated = false;
        }

        public void UpdateHealth(float deltaTime)
        {
            if (invulnerabilityRemaining <= 0f)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            invulnerabilityRemaining = Mathf.Max(0f, invulnerabilityRemaining - safeDeltaTime);
            if (invulnerabilityRemaining > 0f)
            {
                damageFlash.UpdateFlash(safeDeltaTime);
                return;
            }

            damageFlash.StopFlashing();
            StateChanged?.Invoke(GetState());
        }

        public bool TryTakeDamage(int damage)
        {
            if (damage <= 0 || IsInvulnerable || isDefeated)
            {
                return false;
            }

            int previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            invulnerabilityRemaining = settings.DamageInvulnerabilityDuration;
            damageFlash.StartFlashing();
            isDefeated = currentHealth <= 0;
            StateChanged?.Invoke(GetState());
            DamageTaken?.Invoke(new TruckDamageResult(previousHealth - currentHealth, GetState()));

            if (isDefeated)
            {
                Defeated?.Invoke();
            }

            return true;
        }

        public void RestoreState(int savedHealth)
        {
            currentHealth = Mathf.Clamp(savedHealth, 0, settings.MaxHealth);
            invulnerabilityRemaining = 0f;
            isDefeated = currentHealth <= 0;
            damageFlash.StopFlashing();
            StateChanged?.Invoke(GetState());
        }

        public void Respawn()
        {
            currentHealth = settings.MaxHealth;
            invulnerabilityRemaining = settings.DamageInvulnerabilityDuration;
            isDefeated = false;
            damageFlash.StartFlashing();
            StateChanged?.Invoke(GetState());
        }

        public TruckHealthSnapshot GetState()
        {
            return new TruckHealthSnapshot(currentHealth, settings.MaxHealth, IsInvulnerable, isDefeated);
        }
    }

    public readonly struct TruckDamageResult
    {
        public TruckDamageResult(int appliedDamage, TruckHealthSnapshot state)
        {
            AppliedDamage = appliedDamage;
            State = state;
        }

        public int AppliedDamage { get; }
        public TruckHealthSnapshot State { get; }
    }

    public readonly struct TruckHealthSnapshot
    {
        public TruckHealthSnapshot(int currentHealth, int maxHealth, bool isInvulnerable, bool isDefeated)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            IsInvulnerable = isInvulnerable;
            IsDefeated = isDefeated;
        }

        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public bool IsInvulnerable { get; }
        public bool IsDefeated { get; }
    }
}
