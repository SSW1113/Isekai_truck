using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsekaiTruck.Collection
{
    [DisallowMultipleComponent]
    public sealed class MonsterCollectionSystem : MonoBehaviour
    {
        [SerializeField] private MonsterCollectionCatalog catalog;

        private readonly HashSet<string> unlockedMonsterIds = new HashSet<string>();
        private bool isInitialized;

        public MonsterCollectionCatalog Catalog => catalog;

        public event Action<string> MonsterUnlocked;
        public event Action StateChanged;

        public void Initialize()
        {
            if (catalog == null)
            {
                throw new MissingReferenceException("MonsterCollectionSystem에 도감 카탈로그가 연결되지 않았습니다.");
            }

            unlockedMonsterIds.Clear();
            isInitialized = true;
        }

        public bool IsUnlocked(string typeId)
        {
            return !string.IsNullOrWhiteSpace(typeId) && unlockedMonsterIds.Contains(typeId);
        }

        public bool Unlock(string typeId)
        {
            if (!isInitialized || !catalog.TryGetEntry(typeId, out _) || !unlockedMonsterIds.Add(typeId))
            {
                return false;
            }

            MonsterUnlocked?.Invoke(typeId);
            StateChanged?.Invoke();
            return true;
        }

        public List<string> GetUnlockedSnapshot()
        {
            return new List<string>(unlockedMonsterIds);
        }

        public void RestoreState(List<string> unlockedIds)
        {
            unlockedMonsterIds.Clear();
            if (unlockedIds == null)
            {
                return;
            }

            for (int i = 0; i < unlockedIds.Count; i++)
            {
                string typeId = unlockedIds[i];
                if (catalog.TryGetEntry(typeId, out _))
                {
                    unlockedMonsterIds.Add(typeId);
                }
            }
        }

#if UNITY_EDITOR
        public void SetCatalog(MonsterCollectionCatalog targetCatalog)
        {
            catalog = targetCatalog;
        }
#endif
    }
}
