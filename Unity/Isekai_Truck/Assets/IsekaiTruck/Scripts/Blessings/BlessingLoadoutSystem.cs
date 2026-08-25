using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Blessings
{
    [DisallowMultipleComponent]
    public sealed class BlessingLoadoutSystem : MonoBehaviour
    {
        private BlessingSystem blessingSystem;
        private string[] equippedIds;

        public int SlotCount => equippedIds?.Length ?? 0;

        public event Action StateChanged;

        public void Initialize(GameConfig gameConfig, BlessingSystem blessings)
        {
            blessingSystem = blessings;
            equippedIds = new string[gameConfig.Blessing.SlotCount];
        }

        public BlessingDefinition GetEquipped(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount || string.IsNullOrEmpty(equippedIds[slotIndex]))
            {
                return null;
            }

            return blessingSystem.FindDefinition(equippedIds[slotIndex]);
        }

        public int GetEquippedCount(string blessingId)
        {
            int count = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (equippedIds[i] == blessingId)
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryEquip(int slotIndex, string blessingId)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
            {
                return false;
            }

            BlessingDefinition definition = blessingSystem.FindDefinition(blessingId);
            if (definition == null)
            {
                return false;
            }

            if (equippedIds[slotIndex] == blessingId)
            {
                return true;
            }

            if (GetEquippedCount(blessingId) >= blessingSystem.GetOwnedCount(blessingId))
            {
                return false;
            }

            equippedIds[slotIndex] = blessingId;
            StateChanged?.Invoke();
            return true;
        }

        public bool Unequip(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount || string.IsNullOrEmpty(equippedIds[slotIndex]))
            {
                return false;
            }

            equippedIds[slotIndex] = null;
            StateChanged?.Invoke();
            return true;
        }

        public List<string> GetSnapshot()
        {
            return new List<string>(equippedIds);
        }

        public void RestoreState(IReadOnlyList<string> savedEquippedIds)
        {
            Array.Clear(equippedIds, 0, equippedIds.Length);
            if (savedEquippedIds != null)
            {
                int count = Mathf.Min(savedEquippedIds.Count, SlotCount);
                for (int i = 0; i < count; i++)
                {
                    string blessingId = savedEquippedIds[i];
                    if (!string.IsNullOrEmpty(blessingId) && blessingSystem.FindDefinition(blessingId) != null && GetEquippedCount(blessingId) < blessingSystem.GetOwnedCount(blessingId))
                    {
                        equippedIds[i] = blessingId;
                    }
                }
            }

            StateChanged?.Invoke();
        }
    }
}
