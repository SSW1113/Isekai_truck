using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.Enemies;
using IsekaiTruck.Wanted;
using UnityEngine;

namespace IsekaiTruck.Spawn
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        private GameConfig.EnemySettings enemySettings;
        private GameConfig.SpawnSettings spawnSettings;
        private EnemyManager enemyManager;
        private WantedLevelSystem wantedLevelSystem;
        private Transform truck;
        private float lastSpawnTime;

        public int TargetCount => Mathf.Max(
            enemySettings.MinimumCountForTesting,
            wantedLevelSystem.Level * enemySettings.CountPerWantedLevel);

        public void Initialize(GameConfig gameConfig, EnemyManager manager, WantedLevelSystem wanted, Transform truckTransform)
        {
            enemySettings = gameConfig.Enemy;
            spawnSettings = gameConfig.Spawn;
            enemyManager = manager;
            wantedLevelSystem = wanted;
            truck = truckTransform;
            lastSpawnTime = 0f;
        }

        public void FillInitial()
        {
            while (enemyManager.Enemies.Count < TargetCount)
            {
                Spawn();
            }
        }

        public void ReconcileCount()
        {
            RemoveExcessEnemies();
            FillInitial();
        }

        public void UpdateSpawner(float nowMilliseconds)
        {
            RemoveFarEnemies();
            RemoveExcessEnemies();

            if (nowMilliseconds - lastSpawnTime < spawnSettings.SpawnIntervalMilliseconds)
            {
                return;
            }

            lastSpawnTime = nowMilliseconds;
            int needCount = TargetCount - enemyManager.Enemies.Count;
            if (needCount <= 0)
            {
                return;
            }

            int spawnCount = Mathf.Min(needCount, spawnSettings.SpawnPerInterval);
            for (int i = 0; i < spawnCount; i++)
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            string typeId = ChooseEnemyType();
            if (!string.IsNullOrEmpty(typeId))
            {
                enemyManager.CreateEnemy(typeId, GetSpawnPosition());
            }
        }

        private string ChooseEnemyType()
        {
            IReadOnlyDictionary<string, EnemyData> types = enemyManager.Types;
            float totalWeight = 0f;
            string firstTypeId = null;

            foreach (KeyValuePair<string, EnemyData> entry in types)
            {
                firstTypeId ??= entry.Key;
                totalWeight += entry.Value.SpawnWeight;
            }

            if (firstTypeId == null || totalWeight <= 0f)
            {
                return firstTypeId;
            }

            float random = Random.value * totalWeight;
            foreach (KeyValuePair<string, EnemyData> entry in types)
            {
                random -= entry.Value.SpawnWeight;
                if (random <= 0f)
                {
                    return entry.Key;
                }
            }

            return firstTypeId;
        }

        private Vector3 GetSpawnPosition()
        {
            float angle = Random.value * Mathf.PI * 2f;
            float distance = spawnSettings.MinDistance + Random.value * (spawnSettings.MaxDistance - spawnSettings.MinDistance);
            return new Vector3(
                truck.position.x + Mathf.Cos(angle) * distance,
                0f,
                truck.position.z + Mathf.Sin(angle) * distance
            );
        }

        private void RemoveFarEnemies()
        {
            IReadOnlyList<EnemyController> enemies = enemyManager.Enemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Vector3 offset = enemies[i].transform.position - truck.position;
                float distanceSquared = offset.x * offset.x + offset.z * offset.z;
                if (distanceSquared > spawnSettings.DespawnDistance * spawnSettings.DespawnDistance)
                {
                    enemyManager.Remove(enemies[i]);
                }
            }
        }

        private void RemoveExcessEnemies()
        {
            while (enemyManager.Enemies.Count > TargetCount)
            {
                enemyManager.Remove(enemyManager.Enemies[enemyManager.Enemies.Count - 1]);
            }
        }
    }
}
