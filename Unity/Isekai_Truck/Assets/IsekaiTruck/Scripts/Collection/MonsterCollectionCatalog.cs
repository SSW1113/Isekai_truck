using System;
using System.Collections.Generic;
using IsekaiTruck.Monsters;
using UnityEngine;

namespace IsekaiTruck.Collection
{
    [CreateAssetMenu(fileName = "MonsterCollectionCatalog", menuName = "Isekai Truck/Monster Collection Catalog")]
    public sealed class MonsterCollectionCatalog : ScriptableObject
    {
        [SerializeField] private List<MonsterCollectionEntry> entries = new List<MonsterCollectionEntry>();

        public IReadOnlyList<MonsterCollectionEntry> Entries => entries;

        public bool TryGetEntry(string typeId, out MonsterCollectionEntry entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                MonsterCollectionEntry candidate = entries[i];
                if (candidate != null && candidate.TypeId == typeId)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

#if UNITY_EDITOR
        public void SetEntries(List<MonsterCollectionEntry> collectionEntries)
        {
            entries = collectionEntries ?? new List<MonsterCollectionEntry>();
        }
#endif
    }

    [Serializable]
    public sealed class MonsterCollectionEntry
    {
        [SerializeField] private MonsterDefinition monsterDefinition;
        [SerializeField] private Sprite portrait;
        [SerializeField, TextArea(2, 5)] private string behaviorDescription;
        [SerializeField, TextArea(2, 5)] private string defeatDescription;

        public string TypeId => monsterDefinition != null ? monsterDefinition.TypeId : string.Empty;
        public string DisplayName => monsterDefinition != null ? monsterDefinition.DisplayName : string.Empty;
        public Sprite Portrait => portrait;
        public string BehaviorDescription => behaviorDescription;
        public string DefeatDescription => defeatDescription;
        public MonsterDefinition Definition => monsterDefinition;

#if UNITY_EDITOR
        public MonsterCollectionEntry(
            MonsterDefinition definition,
            Sprite standingPortrait,
            string behavior,
            string defeat)
        {
            monsterDefinition = definition;
            portrait = standingPortrait;
            behaviorDescription = behavior;
            defeatDescription = defeat;
        }
#endif
    }
}
