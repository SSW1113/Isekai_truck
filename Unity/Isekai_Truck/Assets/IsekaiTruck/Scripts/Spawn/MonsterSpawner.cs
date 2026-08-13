using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.Monsters;
using UnityEngine;

namespace IsekaiTruck.Spawn
{
    [DisallowMultipleComponent]
    public sealed class MonsterSpawner : MonoBehaviour
    {
        private GameConfig.SpawnSettings settings;
        private MonsterManager monsterManager;
        private Transform truck;
        private float lastSpawnTime;

        public void Initialize(GameConfig gameConfig, MonsterManager manager, Transform truckTransform)
        {
            settings = gameConfig.Spawn;
            monsterManager = manager;
            truck = truckTransform;
            lastSpawnTime = 0f;
        }

        public void FillInitial()
        {
            while (monsterManager.Monsters.Count < settings.TargetCount)
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
            if (currentCount >= settings.TargetCount)
            {
                return;
            }

            int needCount = settings.TargetCount - currentCount;
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

            foreach (KeyValuePair<string, MonsterData> entry in types)
            {
                firstTypeId ??= entry.Key;
                totalWeight += entry.Value.SpawnWeight;
            }

            float random = Random.value * totalWeight;

            foreach (KeyValuePair<string, MonsterData> entry in types)
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
