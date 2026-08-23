using System.Collections.Generic;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using IsekaiTruck.Upgrades;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class GoddessDialogueMockController : MonoBehaviour
    {
        [SerializeField] private GoddessDialogueMockData dialogueData;
        [SerializeField] private GoddessSpeechBubble speechBubble;
        [SerializeField, Min(0.05f)] private float speedCheckInterval = 0.25f;

        private readonly List<int> pendingRuleIndices = new List<int>();
        private PlayerState playerState;
        private TruckController truckController;
        private TruckUpgradeSystem upgradeSystem;
        private PlayerSnapshot playerSnapshot;
        private bool[] hasTriggeredRule;
        private int[] nextMessageIndices;
        private float nextDialogueTime;
        private float nextSpeedCheckTime;

        public void Initialize(PlayerState state, TruckController truck, TruckUpgradeSystem upgrades)
        {
            Unsubscribe();

            playerState = state;
            truckController = truck;
            upgradeSystem = upgrades;
            playerSnapshot = playerState.GetState();

            int ruleCount = dialogueData != null ? dialogueData.Rules.Length : 0;
            hasTriggeredRule = new bool[ruleCount];
            nextMessageIndices = new int[ruleCount];
            pendingRuleIndices.Clear();
            nextDialogueTime = Time.unscaledTime;
            nextSpeedCheckTime = Time.unscaledTime;

            playerState.StateChanged += HandlePlayerStateChanged;
            upgradeSystem.UpgradeApplied += HandleUpgradeApplied;
            QueueRules(GoddessDialogueTrigger.GameStart);
            PlayNextPendingRule(Time.unscaledTime);
        }

        public void SetReferences(GoddessDialogueMockData data, GoddessSpeechBubble targetSpeechBubble)
        {
            dialogueData = data;
            speechBubble = targetSpeechBubble;
        }

        private void Update()
        {
            if (dialogueData == null || speechBubble == null)
            {
                return;
            }

            float currentTime = Time.unscaledTime;
            if (truckController != null && currentTime >= nextSpeedCheckTime)
            {
                nextSpeedCheckTime = currentTime + speedCheckInterval;
                QueueSpeedRules(truckController.CurrentSpeedPerSecond * 3.6f);
            }

            if (currentTime >= nextDialogueTime)
            {
                PlayNextPendingRule(currentTime);
            }
        }

        private void HandlePlayerStateChanged(PlayerSnapshot state)
        {
            if (state.Level > playerSnapshot.Level)
            {
                QueueRules(GoddessDialogueTrigger.LevelUp);
            }
            else if (state.Soul > playerSnapshot.Soul)
            {
                QueueRules(GoddessDialogueTrigger.SoulGained);
            }

            if (playerSnapshot.UpgradePoints <= 0 && state.UpgradePoints > 0)
            {
                QueueRules(GoddessDialogueTrigger.UpgradeAvailable);
            }

            playerSnapshot = state;
            TryPlayPendingRule();
        }

        private void HandleUpgradeApplied(TruckUpgradeResult result)
        {
            QueueRules(GoddessDialogueTrigger.UpgradeApplied);
            TryPlayPendingRule();
        }

        private void TryPlayPendingRule()
        {
            float currentTime = Time.unscaledTime;
            if (currentTime >= nextDialogueTime)
            {
                PlayNextPendingRule(currentTime);
            }
        }

        private void QueueRules(GoddessDialogueTrigger trigger)
        {
            GoddessDialogueRule[] rules = dialogueData != null ? dialogueData.Rules : null;
            if (rules == null)
            {
                return;
            }

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].Trigger == trigger)
                {
                    QueueRule(i);
                }
            }
        }

        private void QueueSpeedRules(float speedKmh)
        {
            GoddessDialogueRule[] rules = dialogueData.Rules;
            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].Trigger == GoddessDialogueTrigger.SpeedReached && speedKmh >= rules[i].Threshold)
                {
                    QueueRule(i);
                }
            }
        }

        private void QueueRule(int ruleIndex)
        {
            GoddessDialogueRule rule = dialogueData.Rules[ruleIndex];
            if ((rule.TriggerOnce && hasTriggeredRule[ruleIndex]) || pendingRuleIndices.Contains(ruleIndex))
            {
                return;
            }

            pendingRuleIndices.Add(ruleIndex);
        }

        private void PlayNextPendingRule(float currentTime)
        {
            int pendingIndex = GetHighestPriorityPendingIndex();
            if (pendingIndex < 0)
            {
                return;
            }

            int ruleIndex = pendingRuleIndices[pendingIndex];
            pendingRuleIndices.RemoveAt(pendingIndex);
            GoddessDialogueRule rule = dialogueData.Rules[ruleIndex];
            string[] messages = rule.Messages;
            if (messages == null || messages.Length == 0)
            {
                return;
            }

            int messageIndex = nextMessageIndices[ruleIndex] % messages.Length;
            nextMessageIndices[ruleIndex] = messageIndex + 1;
            hasTriggeredRule[ruleIndex] = true;
            nextDialogueTime = currentTime + rule.Cooldown;
            speechBubble.ShowMessage(messages[messageIndex]);
        }

        private int GetHighestPriorityPendingIndex()
        {
            int bestPendingIndex = -1;
            int bestPriority = int.MinValue;
            for (int i = 0; i < pendingRuleIndices.Count; i++)
            {
                int priority = dialogueData.Rules[pendingRuleIndices[i]].Priority;
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestPendingIndex = i;
                }
            }

            return bestPendingIndex;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (playerState != null)
            {
                playerState.StateChanged -= HandlePlayerStateChanged;
            }

            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradeApplied -= HandleUpgradeApplied;
            }
        }
    }
}
