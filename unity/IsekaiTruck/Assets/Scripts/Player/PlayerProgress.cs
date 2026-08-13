using System;
using UnityEngine;

namespace IsekaiTruck.Player
{
    public sealed class PlayerProgress : MonoBehaviour
    {
        [Header("Current Run")]
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField] private int defeatedMonsters;
        [SerializeField] private int currentExperience;
        [SerializeField] private int currentSoul;

        [Header("Level Progression")]
        [SerializeField, Min(1)] private int baseRequiredExperience = 100;
        [SerializeField, Min(0)] private int requiredExperienceIncrease = 25;

        public event Action ProgressChanged;

        public int Level => level;
        public int DefeatedMonsters => defeatedMonsters;
        public int CurrentExperience => currentExperience;
        public int CurrentSoul => currentSoul;
        public int RequiredExperience => baseRequiredExperience + (level - 1) * requiredExperienceIncrease;
        public float ExperienceNormalized => RequiredExperience > 0
            ? currentExperience / (float)RequiredExperience
            : 0f;

        public void RegisterMonsterDefeat(int experienceReward, int soulReward)
        {
            int safeExperienceReward = Mathf.Max(0, experienceReward);
            int safeSoulReward = Mathf.Max(0, soulReward);

            defeatedMonsters++;
            AddExperienceInternal(safeExperienceReward);
            currentSoul += safeSoulReward;
            ProgressChanged?.Invoke();

            Debug.Log(
                $"Monster defeated! Count: {defeatedMonsters} / "
                + $"EXP: {currentExperience} (+{safeExperienceReward}) / "
                + $"Soul: {currentSoul} (+{safeSoulReward})");
        }

        public void AddExperience(int amount)
        {
            AddExperienceInternal(Mathf.Max(0, amount));
            ProgressChanged?.Invoke();
        }

        public void AddSoul(int amount)
        {
            currentSoul += Mathf.Max(0, amount);
            ProgressChanged?.Invoke();
        }

        private void AddExperienceInternal(int amount)
        {
            currentExperience += amount;

            while (currentExperience >= RequiredExperience)
            {
                currentExperience -= RequiredExperience;
                level++;
            }
        }

        private void OnValidate()
        {
            level = Mathf.Max(1, level);
            defeatedMonsters = Mathf.Max(0, defeatedMonsters);
            currentExperience = Mathf.Max(0, currentExperience);
            currentSoul = Mathf.Max(0, currentSoul);
            baseRequiredExperience = Mathf.Max(1, baseRequiredExperience);
            requiredExperienceIncrease = Mathf.Max(0, requiredExperienceIncrease);
        }
    }
}
