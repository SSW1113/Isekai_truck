using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.Truck;
using IsekaiTruck.Visuals;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterManager : MonoBehaviour
    {
        [SerializeField] private MonsterPrefabCatalog monsterCatalog;
        [SerializeField] private TextAsset monsterDataFile;
        [SerializeField] private Transform monsterRoot;
        [SerializeField] private SpriteSequenceEffect stunEffectPrefab;
        [SerializeField, Min(0f)] private float stunEffectHeight = 0.15f;

        private readonly List<MonsterController> monsters = new List<MonsterController>();
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private readonly Dictionary<string, MonsterController> monsterPrefabs = new Dictionary<string, MonsterController>();

        private Dictionary<string, MonsterData> monsterTypes;
        private GameConfig.MonsterSettings settings;
        private Transform truck;
        private TruckController truckController;
        private float referenceFrameRate;
        private float areaSlowRadius;
        private float areaSlowMultiplier = 1f;
        private bool isWorldPaused;
        private float pausedTimeMilliseconds;

        public IReadOnlyList<MonsterController> Monsters => monsters;
        public IReadOnlyDictionary<string, MonsterData> Types => monsterTypes;

        public event Action<MonsterData> MonsterDefeated;
        public event Action<MonsterDefeatContext> MonsterDefeatedDetailed;
        public event Action<MonsterCollisionBatch> MonsterCollisionBatchCompleted;

        public void Initialize(GameConfig gameConfig, Transform truckTransform)
        {
            if ((monsterCatalog == null || monsterCatalog.MonsterPrefabs.Count == 0) && monsterDataFile == null)
            {
                throw new MissingReferenceException("MonsterManager에 몬스터 프리팹 카탈로그가 연결되지 않았습니다.");
            }

            settings = gameConfig.Monster;
            referenceFrameRate = gameConfig.ReferenceFrameRate;
            truck = truckTransform;
            truckController = truck.GetComponent<TruckController>();
            monsterRoot = monsterRoot == null ? transform : monsterRoot;
            areaSlowRadius = 0f;
            areaSlowMultiplier = 1f;
            isWorldPaused = false;
            pausedTimeMilliseconds = 0f;
            LoadMonsterTypes();
        }

        public MonsterController CreateMonster(string typeId, float x, float z)
        {
            if (monsterTypes == null || !monsterTypes.TryGetValue(typeId, out MonsterData type))
            {
                Debug.LogError($"존재하지 않는 몬스터 타입: {typeId}", this);
                return null;
            }

            MonsterController monster = monsterPrefabs.TryGetValue(typeId, out MonsterController prefab)
                ? Instantiate(prefab, monsterRoot, false)
                : CreateLegacyMonster(type);

            monster.name = $"Monster ({typeId})";
            monster.transform.position = new Vector3(x, type.Size, z);
            monster.transform.localScale = Vector3.one * type.Size * 2f;
            monster.Initialize(type, truck, GetMonsterTimeMilliseconds(), referenceFrameRate);
            monsters.Add(monster);

            return monster;
        }

        public void Remove(MonsterController monster)
        {
            if (!monsters.Remove(monster))
            {
                return;
            }

            DestroyRuntimeObject(monster.gameObject);
        }

        public void UpdateMonsters(float deltaTime)
        {
            float truckScale = Mathf.Max(truck.localScale.x, truck.localScale.z);
            float extraFleeDistance = settings.CollisionDistance * (truckScale - 1f);
            float collisionDistance = settings.CollisionDistance * truckScale;
            float directionLockDistance = collisionDistance * settings.DirectionLockMultiplier;
            if (isWorldPaused)
            {
                pausedTimeMilliseconds += Mathf.Max(0f, deltaTime) * 1000f;
            }

            float nowMilliseconds = GetMonsterTimeMilliseconds();
            float frameScale = Mathf.Max(deltaTime, 0f) * referenceFrameRate;

            for (int i = 0; i < monsters.Count; i++)
            {
                monsters[i].UpdateMonster(nowMilliseconds, extraFleeDistance, directionLockDistance, frameScale, deltaTime, areaSlowRadius, areaSlowMultiplier, isWorldPaused);
            }

            CheckCollisions(collisionDistance);
        }

        public void SetAreaSpeedModifier(float radius, float speedMultiplier)
        {
            areaSlowRadius = Mathf.Max(0f, radius);
            areaSlowMultiplier = Mathf.Max(0f, speedMultiplier);
        }

        public void SetWorldPaused(bool isPaused)
        {
            isWorldPaused = isPaused;
        }

        public bool StunNearest(Vector3 position, float radius, float duration)
        {
            MonsterController nearest = null;
            float nearestDistanceSquared = radius * radius;

            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterController monster = monsters[i];
                if (monster.IsStunned)
                {
                    continue;
                }

                Vector3 offset = monster.transform.position - position;
                float distanceSquared = offset.x * offset.x + offset.z * offset.z;
                if (distanceSquared <= nearestDistanceSquared)
                {
                    nearest = monster;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            if (nearest == null)
            {
                return false;
            }

            nearest.ApplyStun(duration);
            PlayStunEffect(nearest, duration);
            return true;
        }

        private void PlayStunEffect(MonsterController monster, float duration)
        {
            if (stunEffectPrefab == null || monster == null)
            {
                return;
            }

            Vector3 effectPosition = monster.transform.position + Vector3.up * stunEffectHeight;
            SpriteSequenceEffect effect = Instantiate(stunEffectPrefab, effectPosition, Quaternion.identity);
            effect.transform.SetParent(monster.transform, true);
            effect.PlayForDuration(duration);
        }

        private float GetMonsterTimeMilliseconds()
        {
            return Time.realtimeSinceStartup * 1000f - pausedTimeMilliseconds;
        }

        private void LoadMonsterTypes()
        {
            monsterPrefabs.Clear();

            if (monsterCatalog == null || monsterCatalog.MonsterPrefabs.Count == 0)
            {
                monsterTypes = MonsterJsonLoader.Load(monsterDataFile.text);
                return;
            }

            monsterTypes = new Dictionary<string, MonsterData>();
            IReadOnlyList<MonsterController> prefabs = monsterCatalog.MonsterPrefabs;

            for (int i = 0; i < prefabs.Count; i++)
            {
                MonsterController prefab = prefabs[i];
                if (prefab == null)
                {
                    throw new MissingReferenceException($"몬스터 카탈로그의 {i}번 프리팹이 비어 있습니다.");
                }

                MonsterDefinition definition = prefab.GetComponent<MonsterDefinition>();
                if (definition == null)
                {
                    throw new MissingComponentException($"몬스터 프리팹에 MonsterDefinition이 없습니다: {prefab.name}");
                }

                MonsterData type = definition.CreateData();
                if (string.IsNullOrWhiteSpace(type.Id))
                {
                    throw new InvalidOperationException($"몬스터 프리팹의 Type ID가 비어 있습니다: {prefab.name}");
                }

                if (monsterTypes.ContainsKey(type.Id))
                {
                    throw new InvalidOperationException($"몬스터 프리팹의 Type ID가 중복되었습니다: {type.Id}");
                }

                monsterTypes.Add(type.Id, type);
                monsterPrefabs.Add(type.Id, prefab);
            }
        }

        private MonsterController CreateLegacyMonster(MonsterData type)
        {
            GameObject monsterObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            monsterObject.transform.SetParent(monsterRoot, false);

            MeshRenderer meshRenderer = monsterObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetMaterial(type);

            Collider monsterCollider = monsterObject.GetComponent<Collider>();
            monsterCollider.enabled = false;
            DestroyRuntimeObject(monsterCollider);

            monsterObject.AddComponent<MonsterView>();
            return monsterObject.AddComponent<MonsterController>();
        }

        private void CheckCollisions(float collisionDistance)
        {
            int defeatedCount = 0;
            Vector3 collisionCenter = Vector3.zero;
            float truckScale = Mathf.Max(truck.localScale.x, truck.localScale.z);
            Vector3 knockbackDirection = Vector3.ProjectOnPlane(truck.forward, Vector3.up);
            if (knockbackDirection.sqrMagnitude <= 0.000001f)
            {
                knockbackDirection = Vector3.forward;
            }
            else
            {
                knockbackDirection.Normalize();
            }

            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                MonsterController monster = monsters[i];
                float dx = monster.transform.position.x - truck.position.x;
                float dz = monster.transform.position.z - truck.position.z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance >= collisionDistance)
                {
                    continue;
                }

                MonsterData type = monster.Type;
                Vector3 collisionPosition = monster.transform.position;
                Vector3 contactNormal = truck.position - collisionPosition;
                contactNormal.y = 0f;
                contactNormal = contactNormal.sqrMagnitude > 0.000001f
                    ? contactNormal.normalized
                    : -knockbackDirection;
                MonsterContactResult contactResult = monster.ResolveContact(new MonsterContactContext(
                    truck,
                    truckController,
                    truckScale,
                    collisionDistance,
                    contactNormal,
                    knockbackDirection
                ));
                if (contactResult.Outcome != MonsterContactOutcome.Defeated)
                {
                    continue;
                }

                monsters.RemoveAt(i);
                monster.BeginDefeat(knockbackDirection);
                defeatedCount++;
                collisionCenter += collisionPosition;

                Debug.Log($"{type.Name} 처치!", this);
                MonsterDefeatedDetailed?.Invoke(new MonsterDefeatContext(type, collisionPosition, knockbackDirection));
                MonsterDefeated?.Invoke(type);
            }

            if (defeatedCount > 0)
            {
                MonsterCollisionBatchCompleted?.Invoke(new MonsterCollisionBatch(
                    defeatedCount,
                    collisionCenter / defeatedCount,
                    knockbackDirection
                ));
            }
        }

        private Material GetMaterial(MonsterData type)
        {
            if (materials.TryGetValue(type.Id, out Material material))
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = $"Monster Material ({type.Id})",
                color = type.Color
            };

            materials.Add(type.Id, material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in materials.Values)
            {
                DestroyRuntimeObject(material);
            }

            materials.Clear();
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
        public void SetCatalog(MonsterPrefabCatalog catalog)
        {
            monsterCatalog = catalog;
        }

        public void SetDataFile(TextAsset dataFile)
        {
            monsterDataFile = dataFile;
        }

        public void SetStunEffect(SpriteSequenceEffect effectPrefab, float height)
        {
            stunEffectPrefab = effectPrefab;
            stunEffectHeight = Mathf.Max(0f, height);
        }
#endif
    }

    public readonly struct MonsterDefeatContext
    {
        public MonsterDefeatContext(MonsterData type, Vector3 worldPosition, Vector3 knockbackDirection)
        {
            Type = type;
            WorldPosition = worldPosition;
            KnockbackDirection = knockbackDirection;
        }

        public MonsterData Type { get; }
        public Vector3 WorldPosition { get; }
        public Vector3 KnockbackDirection { get; }
    }

    public readonly struct MonsterCollisionBatch
    {
        public MonsterCollisionBatch(int count, Vector3 worldPosition, Vector3 direction)
        {
            Count = count;
            WorldPosition = worldPosition;
            Direction = direction;
        }

        public int Count { get; }
        public Vector3 WorldPosition { get; }
        public Vector3 Direction { get; }
    }
}
