using System.Collections.Generic;
using UnityEngine;

namespace IsekaiTruck.Enemies
{
    [CreateAssetMenu(fileName = "EnemyPrefabCatalog", menuName = "Isekai Truck/Enemy Prefab Catalog")]
    public sealed class EnemyPrefabCatalog : ScriptableObject
    {
        [SerializeField] private List<EnemyController> enemyPrefabs = new List<EnemyController>();

        public IReadOnlyList<EnemyController> EnemyPrefabs => enemyPrefabs;

#if UNITY_EDITOR
        public void SetPrefabs(List<EnemyController> prefabs)
        {
            enemyPrefabs = prefabs;
        }
#endif
    }
}
