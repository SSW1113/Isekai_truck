using System.Collections.Generic;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [CreateAssetMenu(fileName = "MonsterPrefabCatalog", menuName = "Isekai Truck/Monster Prefab Catalog")]
    public sealed class MonsterPrefabCatalog : ScriptableObject
    {
        [SerializeField] private List<MonsterController> monsterPrefabs = new List<MonsterController>();

        public IReadOnlyList<MonsterController> MonsterPrefabs => monsterPrefabs;

#if UNITY_EDITOR
        public void SetPrefabs(List<MonsterController> prefabs)
        {
            monsterPrefabs = prefabs;
        }
#endif
    }
}
