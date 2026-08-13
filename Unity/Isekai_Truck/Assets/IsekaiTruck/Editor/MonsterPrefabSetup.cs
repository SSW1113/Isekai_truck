using System;
using System.Collections.Generic;
using System.IO;
using IsekaiTruck.Config;
using IsekaiTruck.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class MonsterPrefabSetup
    {
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const string PrefabFolderPath = "Assets/IsekaiTruck/Prefabs/Monsters";
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";

        [MenuItem("Isekai Truck/Setup Monster Prefabs")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            TextAsset monsterDataFile = AssetDatabase.LoadAssetAtPath<TextAsset>(MonsterDataPath);
            if (monsterDataFile == null)
            {
                throw new InvalidOperationException($"몬스터 데이터를 불러오지 못했습니다: {MonsterDataPath}");
            }

            EnsureFolder(PrefabFolderPath);
            Dictionary<string, MonsterData> types = MonsterJsonLoader.Load(monsterDataFile.text);

            foreach (KeyValuePair<string, MonsterData> entry in types)
            {
                string prefabPath = $"{PrefabFolderPath}/{ToPrefabName(entry.Key)}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    CreatePrefab(prefabPath, entry.Value);
                }

                EnsureViewComponent(prefabPath);
            }

            MonsterPrefabCatalog catalog = GetOrCreateCatalog();
            RefreshCatalog(catalog);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();
            if (monsterManager == null)
            {
                throw new InvalidOperationException("Main 씬에서 MonsterManager를 찾지 못했습니다.");
            }

            monsterManager.SetCatalog(catalog);
            EditorUtility.SetDirty(monsterManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "기존 몬스터를 프리팹으로 만들고 Main 씬에 카탈로그를 연결했습니다.",
                    "확인"
                );
            }
        }

        [MenuItem("Isekai Truck/Create Monster Prefab...")]
        public static void CreateNewPrefab()
        {
            EnsureFolder(PrefabFolderPath);
            string prefabPath = EditorUtility.SaveFilePanelInProject(
                "새 몬스터 프리팹",
                "NewMonster",
                "prefab",
                "몬스터 프리팹을 저장할 위치를 선택하세요.",
                PrefabFolderPath
            );

            if (string.IsNullOrEmpty(prefabPath))
            {
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            string typeId = ToTypeId(fileName);
            MonsterData type = new MonsterData(
                typeId,
                fileName,
                "#FFFFFF",
                Color.white,
                0.6f,
                0.04f,
                7f,
                50,
                2,
                1f
            );

            GameObject prefab = CreatePrefab(prefabPath, type);
            MonsterPrefabCatalog catalog = GetOrCreateCatalog();
            RefreshCatalog(catalog);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        public static void RefreshCatalog(MonsterPrefabCatalog catalog)
        {
            EnsureFolder(PrefabFolderPath);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolderPath });
            List<MonsterController> prefabs = new List<MonsterController>();

            Array.Sort(prefabGuids, (left, right) => string.CompareOrdinal(
                AssetDatabase.GUIDToAssetPath(left),
                AssetDatabase.GUIDToAssetPath(right)
            ));

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EnsureViewComponent(path);
                GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MonsterController controller = prefabObject == null ? null : prefabObject.GetComponent<MonsterController>();
                MonsterDefinition definition = prefabObject == null ? null : prefabObject.GetComponent<MonsterDefinition>();
                MonsterView monsterView = prefabObject == null ? null : prefabObject.GetComponent<MonsterView>();

                if (controller == null || definition == null || monsterView == null)
                {
                    Debug.LogWarning($"필수 몬스터 컴포넌트가 없어 카탈로그에서 제외했습니다: {path}");
                    continue;
                }

                prefabs.Add(controller);
            }

            catalog.SetPrefabs(prefabs);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        public static void Verify()
        {
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (catalog == null || catalog.MonsterPrefabs.Count == 0 || config == null)
            {
                throw new InvalidOperationException("몬스터 프리팹 카탈로그가 비어 있습니다.");
            }

            HashSet<string> typeIds = new HashSet<string>();
            for (int i = 0; i < catalog.MonsterPrefabs.Count; i++)
            {
                MonsterController prefab = catalog.MonsterPrefabs[i];
                MonsterDefinition definition = prefab == null ? null : prefab.GetComponent<MonsterDefinition>();
                MonsterView monsterView = prefab == null ? null : prefab.GetComponent<MonsterView>();

                if (definition == null || monsterView == null || string.IsNullOrWhiteSpace(definition.TypeId))
                {
                    throw new InvalidOperationException($"카탈로그의 {i}번 몬스터 정의가 잘못되었습니다.");
                }

                if (!typeIds.Add(definition.TypeId))
                {
                    throw new InvalidOperationException($"몬스터 Type ID가 중복되었습니다: {definition.TypeId}");
                }
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();
            SerializedObject serializedManager = new SerializedObject(monsterManager);
            serializedManager.Update();

            if (serializedManager.FindProperty("monsterCatalog").objectReferenceValue != catalog)
            {
                throw new InvalidOperationException("Main 씬 MonsterManager에 프리팹 카탈로그가 연결되지 않았습니다.");
            }

            GameObject truckObject = new GameObject("Monster Prefab Verification Truck");
            GameObject managerObject = new GameObject("Monster Prefab Verification Manager");

            try
            {
                MonsterManager runtimeManager = managerObject.AddComponent<MonsterManager>();
                runtimeManager.SetCatalog(catalog);
                runtimeManager.Initialize(config, truckObject.transform);

                for (int i = 0; i < catalog.MonsterPrefabs.Count; i++)
                {
                    MonsterDefinition definition = catalog.MonsterPrefabs[i].GetComponent<MonsterDefinition>();
                    MonsterController monster = runtimeManager.CreateMonster(definition.TypeId, i * 3f, 0f);

                    if (monster == null || monster.GetComponent<MonsterDefinition>() == null ||
                        monster.GetComponent<MonsterView>() == null || monster.Type.Id != definition.TypeId)
                    {
                        throw new InvalidOperationException($"프리팹 몬스터 생성에 실패했습니다: {definition.TypeId}");
                    }

                    if (Mathf.Abs(monster.transform.position.y - definition.Size) > 0.0001f ||
                        Mathf.Abs(monster.transform.localScale.x - definition.Size * 2f) > 0.0001f)
                    {
                        throw new InvalidOperationException($"프리팹 몬스터 크기 적용에 실패했습니다: {definition.TypeId}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(truckObject);
            }

            Debug.Log($"Monster prefab setup verification passed. Prefabs: {catalog.MonsterPrefabs.Count}");
        }

        private static MonsterPrefabCatalog GetOrCreateCatalog()
        {
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<MonsterPrefabCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static GameObject CreatePrefab(string prefabPath, MonsterData type)
        {
            GameObject monsterObject = new GameObject(type.Name);
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualObject.name = "Visual";
            visualObject.transform.SetParent(monsterObject.transform, false);
            monsterObject.name = type.Name;

            Collider collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MonsterDefinition definition = monsterObject.AddComponent<MonsterDefinition>();
            definition.Configure(type);
            MonsterView monsterView = monsterObject.AddComponent<MonsterView>();
            monsterView.SetVisualRoot(visualObject.transform);
            monsterObject.AddComponent<MonsterController>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(monsterObject, prefabPath);
            Object.DestroyImmediate(monsterObject);
            return prefab;
        }

        private static bool EnsureViewComponent(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            bool isChanged = false;

            try
            {
                MonsterView monsterView = root.GetComponent<MonsterView>();

                if (monsterView == null)
                {
                    monsterView = root.AddComponent<MonsterView>();
                    isChanged = true;
                }

                if (monsterView.VisualRoot == null)
                {
                    monsterView.SetVisualRoot(root.transform);
                    isChanged = true;
                }

                if (isChanged)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }

                return isChanged;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }

        private static string ToPrefabName(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                return "Monster";
            }

            return char.ToUpperInvariant(typeId[0]) + typeId.Substring(1);
        }

        private static string ToTypeId(string value)
        {
            return value.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }

    [CustomEditor(typeof(MonsterPrefabCatalog))]
    public sealed class MonsterPrefabCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (GUILayout.Button("프리팹 폴더에서 목록 새로고침"))
            {
                MonsterPrefabSetup.RefreshCatalog((MonsterPrefabCatalog)target);
            }

            EditorGUILayout.HelpBox(
                "Assets/IsekaiTruck/Prefabs/Monsters 폴더의 프리팹을 카탈로그에 등록합니다.",
                MessageType.Info
            );
        }
    }

    [CustomEditor(typeof(MonsterDefinition))]
    public sealed class MonsterDefinitionEditor : UnityEditor.Editor
    {
        private const string PrefabFolderPath = "Assets/IsekaiTruck/Prefabs/Monsters";

        private SerializedProperty typeId;
        private SerializedProperty displayName;
        private SerializedProperty color;
        private SerializedProperty size;
        private SerializedProperty speed;
        private SerializedProperty fleeDistance;
        private SerializedProperty exp;
        private SerializedProperty soul;
        private SerializedProperty spawnWeight;

        private void OnEnable()
        {
            typeId = serializedObject.FindProperty("typeId");
            displayName = serializedObject.FindProperty("displayName");
            color = serializedObject.FindProperty("color");
            size = serializedObject.FindProperty("size");
            speed = serializedObject.FindProperty("speed");
            fleeDistance = serializedObject.FindProperty("fleeDistance");
            exp = serializedObject.FindProperty("exp");
            soul = serializedObject.FindProperty("soul");
            spawnWeight = serializedObject.FindProperty("spawnWeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("몬스터 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(typeId, new GUIContent("타입 ID", "몬스터를 구분하는 고유 ID입니다."));
            EditorGUILayout.PropertyField(displayName, new GUIContent("표시 이름"));
            EditorGUILayout.PropertyField(color, new GUIContent("기본 색상"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("개별 스탯", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(size, new GUIContent("크기"));
            EditorGUILayout.PropertyField(speed, new GUIContent("이동 속도"));
            EditorGUILayout.PropertyField(fleeDistance, new GUIContent("도망 감지 거리"));
            EditorGUILayout.PropertyField(exp, new GUIContent("처치 EXP"));
            EditorGUILayout.PropertyField(soul, new GUIContent("처치 영혼"));
            EditorGUILayout.PropertyField(spawnWeight, new GUIContent("스폰 가중치"));

            serializedObject.ApplyModifiedProperties();
            DrawValidation((MonsterDefinition)target);
        }

        private void DrawValidation(MonsterDefinition definition)
        {
            string currentTypeId = definition.TypeId == null ? string.Empty : definition.TypeId.Trim();

            if (string.IsNullOrEmpty(currentTypeId))
            {
                EditorGUILayout.HelpBox("타입 ID를 입력해야 스폰 목록에 등록할 수 있습니다.", MessageType.Error);
            }
            else if (TryFindDuplicateTypeId(definition, currentTypeId, out string duplicatePath))
            {
                EditorGUILayout.HelpBox($"같은 타입 ID를 사용하는 프리팹이 있습니다: {duplicatePath}", MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                EditorGUILayout.HelpBox("표시 이름이 비어 있습니다.", MessageType.Warning);
            }

            if (definition.SpawnWeight <= 0f)
            {
                EditorGUILayout.HelpBox("스폰 가중치가 0이면 이 몬스터는 일반 스폰에서 선택되지 않습니다.", MessageType.Info);
            }

            if (definition.GetComponent<MonsterController>() == null || definition.GetComponent<MonsterView>() == null)
            {
                EditorGUILayout.HelpBox("MonsterController 또는 MonsterView가 없어 카탈로그에 등록되지 않습니다.", MessageType.Error);
            }
        }

        private static bool TryFindDuplicateTypeId(MonsterDefinition current, string typeId, out string duplicatePath)
        {
            string currentPath = AssetDatabase.GetAssetPath(current.gameObject);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolderPath });

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (prefabPath == currentPath)
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                MonsterDefinition definition = prefab == null ? null : prefab.GetComponent<MonsterDefinition>();

                string otherTypeId = definition == null || definition.TypeId == null ? string.Empty : definition.TypeId.Trim();
                if (string.Equals(otherTypeId, typeId, StringComparison.Ordinal))
                {
                    duplicatePath = prefabPath;
                    return true;
                }
            }

            duplicatePath = null;
            return false;
        }
    }

    [CustomEditor(typeof(MonsterView))]
    public sealed class MonsterViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox(
                "모델은 Visual Root 아래에 배치하세요. 텍스처 색상을 유지하려면 Apply Definition Color를 끄고, " +
                "Animator에는 선택적으로 IsFleeing(bool)과 MoveSpeed(float) 파라미터를 만들 수 있습니다.",
                MessageType.Info
            );
        }
    }
}
