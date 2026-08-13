using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterManager : MonoBehaviour
    {
        [SerializeField] private TextAsset monsterDataFile;
        [SerializeField] private Transform monsterRoot;

        private readonly List<MonsterController> monsters = new List<MonsterController>();
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        private Dictionary<string, MonsterData> monsterTypes;
        private GameConfig.MonsterSettings settings;
        private Transform truck;

        public IReadOnlyList<MonsterController> Monsters => monsters;
        public IReadOnlyDictionary<string, MonsterData> Types => monsterTypes;

        public event Action<MonsterData> MonsterDefeated;

        public void Initialize(GameConfig gameConfig, Transform truckTransform)
        {
            if (monsterDataFile == null)
            {
                throw new MissingReferenceException("MonsterManager에 monsters.json이 연결되지 않았습니다.");
            }

            settings = gameConfig.Monster;
            truck = truckTransform;
            monsterRoot = monsterRoot == null ? transform : monsterRoot;
            monsterTypes = MonsterJsonLoader.Load(monsterDataFile.text);
        }

        public MonsterController CreateMonster(string typeId, float x, float z)
        {
            if (monsterTypes == null || !monsterTypes.TryGetValue(typeId, out MonsterData type))
            {
                Debug.LogError($"존재하지 않는 몬스터 타입: {typeId}", this);
                return null;
            }

            GameObject monsterObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            monsterObject.name = $"Monster ({typeId})";
            monsterObject.transform.SetParent(monsterRoot, false);
            monsterObject.transform.position = new Vector3(x, type.Size, z);
            monsterObject.transform.localScale = Vector3.one * type.Size * 2f;

            MeshRenderer meshRenderer = monsterObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetMaterial(type);

            Collider monsterCollider = monsterObject.GetComponent<Collider>();
            monsterCollider.enabled = false;
            DestroyRuntimeObject(monsterCollider);

            MonsterController monster = monsterObject.AddComponent<MonsterController>();
            monster.Initialize(type, truck, Time.realtimeSinceStartup * 1000f);
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

        public void UpdateMonsters()
        {
            float truckScale = Mathf.Max(truck.localScale.x, truck.localScale.z);
            float extraFleeDistance = settings.CollisionDistance * (truckScale - 1f);
            float collisionDistance = settings.CollisionDistance * truckScale;
            float directionLockDistance = collisionDistance * settings.DirectionLockMultiplier;
            float nowMilliseconds = Time.realtimeSinceStartup * 1000f;

            for (int i = 0; i < monsters.Count; i++)
            {
                monsters[i].UpdateMonster(nowMilliseconds, extraFleeDistance, directionLockDistance);
            }

            CheckCollisions(collisionDistance);
        }

        private void CheckCollisions(float collisionDistance)
        {
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
                monsters.RemoveAt(i);
                DestroyRuntimeObject(monster.gameObject);

                Debug.Log($"{type.Name} 처치!", this);
                MonsterDefeated?.Invoke(type);
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
        public void SetDataFile(TextAsset dataFile)
        {
            monsterDataFile = dataFile;
        }
#endif
    }
}
