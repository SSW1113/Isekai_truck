using System;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [CreateAssetMenu(fileName = "GoddessDialogueMockData", menuName = "Isekai Truck/Goddess Dialogue Mock Data")]
    public sealed class GoddessDialogueMockData : ScriptableObject
    {
        [SerializeField] private GoddessDialogueRule[] rules = Array.Empty<GoddessDialogueRule>();

        public GoddessDialogueRule[] Rules => rules;

#if UNITY_EDITOR
        public void SetRules(GoddessDialogueRule[] configuredRules)
        {
            rules = configuredRules ?? Array.Empty<GoddessDialogueRule>();
        }
#endif
    }

    [Serializable]
    public sealed class GoddessDialogueRule
    {
        [SerializeField] private GoddessDialogueTrigger trigger;
        [SerializeField, Min(0f)] private float threshold;
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [SerializeField] private int priority;
        [SerializeField] private bool triggerOnce;
        [SerializeField] private string[] messages = Array.Empty<string>();

        public GoddessDialogueRule(
            GoddessDialogueTrigger dialogueTrigger,
            float triggerThreshold,
            float dialogueCooldown,
            int dialoguePriority,
            bool shouldTriggerOnce,
            params string[] dialogueMessages
        )
        {
            trigger = dialogueTrigger;
            threshold = triggerThreshold;
            cooldown = dialogueCooldown;
            priority = dialoguePriority;
            triggerOnce = shouldTriggerOnce;
            messages = dialogueMessages ?? Array.Empty<string>();
        }

        public GoddessDialogueTrigger Trigger => trigger;
        public float Threshold => threshold;
        public float Cooldown => cooldown;
        public int Priority => priority;
        public bool TriggerOnce => triggerOnce;
        public string[] Messages => messages;
    }

    public enum GoddessDialogueTrigger
    {
        GameStart,
        LevelUp,
        SoulGained,
        UpgradeAvailable,
        UpgradeApplied,
        SpeedReached
    }
}
