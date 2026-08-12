using System.Collections.Generic;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using UnityEngine;

namespace IsekaiTruck.Spawning
{
    public sealed class SpawnManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MonsterController monsterPrefab;
        [SerializeField] private Transform truck;
        [SerializeField] private Transform monstersParent;
        [SerializeField] private PlayerProgress playerProgress;

        [Header("Population")]
        [SerializeField, Min(1)] private int targetCount = 15;
        [SerializeField, Min(0.01f)] private float spawnInterval = 0.25f;
        [SerializeField, Min(1)] private int spawnPerInterval = 1;

        [Header("Distances")]
        [SerializeField, Min(0f)] private float minSpawnDistance = 18f;
        [SerializeField, Min(0f)] private float maxSpawnDistance = 30f;
        [SerializeField, Min(0f)] private float despawnDistance = 50f;
        [SerializeField] private float spawnHeight = 0.5f;

        private readonly List<MonsterController> monsters = new List<MonsterController>();
        private float nextSpawnTime;

        public void Configure(
            MonsterController prefab,
            Transform truckTarget,
            Transform parent,
            PlayerProgress progress)
        {
            monsterPrefab = prefab;
            truck = truckTarget;
            monstersParent = parent;
            playerProgress = progress;
        }

        private void Start()
        {
            nextSpawnTime = Time.time;
        }

        private void Update()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

            RemoveMissingAndDistantMonsters();

            if (monsters.Count >= targetCount || Time.time < nextSpawnTime)
            {
                return;
            }

            int amount = Mathf.Min(spawnPerInterval, targetCount - monsters.Count);
            for (int index = 0; index < amount; index++)
            {
                SpawnMonster();
            }

            nextSpawnTime = Time.time + spawnInterval;
        }

        private bool HasRequiredReferences()
        {
            return monsterPrefab != null && truck != null && monstersParent != null;
        }

        private void SpawnMonster()
        {
            float minimum = Mathf.Min(minSpawnDistance, maxSpawnDistance);
            float maximum = Mathf.Max(minSpawnDistance, maxSpawnDistance);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minimum, maximum);

            Vector3 spawnPosition = truck.position + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance);
            spawnPosition.y = spawnHeight;

            MonsterController monster = Instantiate(
                monsterPrefab,
                spawnPosition,
                Quaternion.identity,
                monstersParent);
            monster.name = "Monster";
            monster.Configure(truck, playerProgress);
            monsters.Add(monster);
        }

        private void RemoveMissingAndDistantMonsters()
        {
            float sqrDespawnDistance = despawnDistance * despawnDistance;

            for (int index = monsters.Count - 1; index >= 0; index--)
            {
                MonsterController monster = monsters[index];
                if (monster == null)
                {
                    monsters.RemoveAt(index);
                    continue;
                }

                Vector3 offset = monster.transform.position - truck.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= sqrDespawnDistance)
                {
                    continue;
                }

                monsters.RemoveAt(index);
                Destroy(monster.gameObject);
            }
        }

        private void OnValidate()
        {
            maxSpawnDistance = Mathf.Max(maxSpawnDistance, minSpawnDistance);
            despawnDistance = Mathf.Max(despawnDistance, maxSpawnDistance);
            spawnPerInterval = Mathf.Max(1, spawnPerInterval);
            targetCount = Mathf.Max(1, targetCount);
        }
    }
}
