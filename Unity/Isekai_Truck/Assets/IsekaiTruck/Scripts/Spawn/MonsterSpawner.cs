using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.Monsters;
using IsekaiTruck.Wanted;
using UnityEngine;

namespace IsekaiTruck.Spawn
{
    [DisallowMultipleComponent]
    public sealed class MonsterSpawner : MonoBehaviour
    {
        private GameConfig.SpawnSettings settings;
        private MonsterManager monsterManager;
        private WantedLevelSystem wantedLevelSystem;
        private Transform truck;
        private float lastSpawnTime;

        public int TargetCount => settings.GetTargetCount(wantedLevelSystem?.Level ?? 0);

        public void Initialize(GameConfig gameConfig, MonsterManager manager, Transform truckTransform)
        {
            Initialize(gameConfig, manager, truckTransform, null);
        }

        public void Initialize(GameConfig gameConfig, MonsterManager manager, Transform truckTransform, WantedLevelSystem wanted)
        {
            settings = gameConfig.Spawn;
            monsterManager = manager;
            wantedLevelSystem = wanted;
            truck = truckTransform;
            lastSpawnTime = 0f;
        }

        public void FillInitial()
        {
            while (monsterManager.Monsters.Count < TargetCount)
            {
                Spawn();
            }
        }

        public void UpdateSpawner(float nowMilliseconds)
        {
            RemoveFarMonsters();

            if (nowMilliseconds - lastSpawnTime < settings.SpawnIntervalMilliseconds)
            {
                return;
            }

            lastSpawnTime = nowMilliseconds;

            int currentCount = monsterManager.Monsters.Count;
            if (currentCount >= TargetCount)
            {
                return;
            }

            int needCount = TargetCount - currentCount;
            int spawnCount = Mathf.Min(needCount, settings.SpawnPerInterval);

            for (int i = 0; i < spawnCount; i++)
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            string typeId = ChooseMonsterType();
            Vector3 position = GetSpawnPosition();
            monsterManager.CreateMonster(typeId, position.x, position.z);
        }

        private string ChooseMonsterType()
        {
            IReadOnlyDictionary<string, MonsterData> types = monsterManager.Types;
            float totalWeight = 0f;
            string firstTypeId = null;
            int wantedLevel = wantedLevelSystem?.Level ?? 0;

            foreach (KeyValuePair<string, MonsterData> entry in types)
            {
                float weight = settings.GetSpawnWeight(entry.Key, entry.Value.SpawnWeight, wantedLevel);
                if (weight <= 0f)
                {
                    continue;
                }

                firstTypeId ??= entry.Key;
                totalWeight += weight;
            }

            if (firstTypeId == null || totalWeight <= 0f)
            {
                return firstTypeId;
            }

            float random = Random.value * totalWeight;

            foreach (KeyValuePair<string, MonsterData> entry in types)
            {
                float weight = settings.GetSpawnWeight(entry.Key, entry.Value.SpawnWeight, wantedLevel);
                if (weight <= 0f)
                {
                    continue;
                }

                random -= weight;

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
            float distance = settings.MinDistance + Random.value * (settings.MaxDistance - settings.MinDistance);

            return new Vector3(
                truck.position.x + Mathf.Cos(angle) * distance,
                0f,
                truck.position.z + Mathf.Sin(angle) * distance
            );
        }

        private void RemoveFarMonsters()
        {
            IReadOnlyList<MonsterController> monsters = monsterManager.Monsters;

            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                MonsterController monster = monsters[i];
                float dx = monster.transform.position.x - truck.position.x;
                float dz = monster.transform.position.z - truck.position.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance > settings.DespawnDistance)
                {
                    monsterManager.Remove(monster);
                }
            }
        }
    }
}
