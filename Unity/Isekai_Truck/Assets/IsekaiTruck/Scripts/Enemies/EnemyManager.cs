using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.Truck;
using IsekaiTruck.Wanted;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyManager : MonoBehaviour
    {
        [SerializeField] private EnemyPrefabCatalog enemyCatalog;
        [SerializeField] private Transform enemyRoot;

        private readonly List<EnemyController> enemies = new List<EnemyController>();
        private readonly Dictionary<string, EnemyData> enemyTypes = new Dictionary<string, EnemyData>();
        private readonly Dictionary<string, EnemyController> enemyPrefabs = new Dictionary<string, EnemyController>();

        private GameConfig.EnemySettings settings;
        private Transform truck;
        private TruckHealthController truckHealth;
        private WantedLevelSystem wantedLevelSystem;
        private bool isWorldPaused;

        public IReadOnlyList<EnemyController> Enemies => enemies;
        public IReadOnlyDictionary<string, EnemyData> Types => enemyTypes;
        public bool IsWorldPaused => isWorldPaused;

        public void Initialize(GameConfig gameConfig, Transform truckTransform, TruckHealthController healthController)
        {
            Initialize(gameConfig, truckTransform, healthController, null);
        }

        public void Initialize(GameConfig gameConfig, Transform truckTransform, TruckHealthController healthController, WantedLevelSystem wanted)
        {
            if (enemyCatalog == null || enemyCatalog.EnemyPrefabs.Count == 0)
            {
                throw new MissingReferenceException("EnemyManager에 적 프리팹 카탈로그가 연결되지 않았습니다.");
            }

            settings = gameConfig.Enemy;
            truck = truckTransform;
            truckHealth = healthController;
            wantedLevelSystem = wanted;
            enemyRoot = enemyRoot == null ? transform : enemyRoot;
            isWorldPaused = false;
            LoadEnemyTypes();
        }

        public EnemyController CreateEnemy(string typeId, Vector3 position)
        {
            if (!enemyTypes.TryGetValue(typeId, out EnemyData type) || !enemyPrefabs.TryGetValue(typeId, out EnemyController prefab))
            {
                Debug.LogError($"존재하지 않는 적 타입: {typeId}", this);
                return null;
            }

            EnemyController enemy = Instantiate(prefab, enemyRoot, false);
            enemy.name = $"Enemy ({type.DisplayName})";
            enemy.transform.position = new Vector3(position.x, type.Size * 0.5f, position.z);
            enemy.transform.localScale = Vector3.one * type.Size;
            enemy.Initialize(type, truck);
            enemies.Add(enemy);
            return enemy;
        }

        public void Remove(EnemyController enemy)
        {
            if (!enemies.Remove(enemy))
            {
                return;
            }

            DestroyRuntimeObject(enemy.gameObject);
        }

        public void UpdateEnemies(float deltaTime)
        {
            float speedMultiplier = wantedLevelSystem != null && wantedLevelSystem.Level >= settings.WantedSpeedBoostLevel
                ? settings.WantedSpeedMultiplier
                : 1f;
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].UpdateEnemy(deltaTime, isWorldPaused, speedMultiplier);
            }

            if (!isWorldPaused)
            {
                CheckTruckContacts();
            }
        }

        public void SetWorldPaused(bool isPaused)
        {
            isWorldPaused = isPaused;
        }

        private void CheckTruckContacts()
        {
            float truckScale = Mathf.Max(truck.localScale.x, truck.localScale.z);
            float truckCollisionRadius = settings.TruckCollisionRadius * truckScale;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                Vector3 offset = enemy.transform.position - truck.position;
                float collisionDistance = truckCollisionRadius + enemy.Type.CollisionRadius * enemy.Type.Size;
                float distanceSquared = offset.x * offset.x + offset.z * offset.z;
                if (distanceSquared < collisionDistance * collisionDistance)
                {
                    truckHealth.TryTakeDamage(enemy.Type.ContactDamage);
                }
            }
        }

        private void LoadEnemyTypes()
        {
            enemyTypes.Clear();
            enemyPrefabs.Clear();
            IReadOnlyList<EnemyController> prefabs = enemyCatalog.EnemyPrefabs;

            for (int i = 0; i < prefabs.Count; i++)
            {
                EnemyController prefab = prefabs[i];
                if (prefab == null)
                {
                    throw new MissingReferenceException($"적 카탈로그의 {i}번 프리팹이 비어 있습니다.");
                }

                EnemyDefinition definition = prefab.GetComponent<EnemyDefinition>();
                if (definition == null)
                {
                    throw new MissingComponentException($"적 프리팹에 EnemyDefinition이 없습니다: {prefab.name}");
                }

                EnemyData type = definition.CreateData();
                if (string.IsNullOrWhiteSpace(type.Id))
                {
                    throw new InvalidOperationException($"적 프리팹의 Type ID가 비어 있습니다: {prefab.name}");
                }

                if (enemyTypes.ContainsKey(type.Id))
                {
                    throw new InvalidOperationException($"적 프리팹의 Type ID가 중복되었습니다: {type.Id}");
                }

                enemyTypes.Add(type.Id, type);
                enemyPrefabs.Add(type.Id, prefab);
            }
        }

        private static void DestroyRuntimeObject(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(target);
                return;
            }
#endif
            Destroy(target);
        }

#if UNITY_EDITOR
        public void SetCatalog(EnemyPrefabCatalog catalog)
        {
            enemyCatalog = catalog;
        }
#endif
    }
}
