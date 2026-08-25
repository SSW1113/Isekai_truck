using System;
using IsekaiTruck.Monsters;
using UnityEditor;
using UnityEngine;

namespace IsekaiTruck.Editor
{
    public static class ResidentCommonStatsSetup
    {
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";

        [MenuItem("Isekai Truck/Apply Resident Common Stats")]
        public static void Setup()
        {
            MonsterPrefabCatalog catalog = LoadCatalog();

            for (int index = 0; index < catalog.MonsterPrefabs.Count; index++)
            {
                MonsterController prefab = catalog.MonsterPrefabs[index];
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Resident catalog entry is missing: {index}");
                }

                string prefabPath = AssetDatabase.GetAssetPath(prefab.gameObject);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    MonsterDefinition definition = root.GetComponent<MonsterDefinition>();
                    if (definition == null)
                    {
                        throw new InvalidOperationException($"Resident definition is missing: {prefabPath}");
                    }

                    definition.ApplyDefaultCommonStats();
                    EditorUtility.SetDirty(definition);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "모든 주민의 공통 능력치를 적용했습니다.", "확인");
            }
        }

        [MenuItem("Isekai Truck/Verify Resident Common Stats")]
        public static void Verify()
        {
            MonsterPrefabCatalog catalog = LoadCatalog();

            for (int index = 0; index < catalog.MonsterPrefabs.Count; index++)
            {
                MonsterController prefab = catalog.MonsterPrefabs[index];
                MonsterDefinition definition = prefab != null
                    ? prefab.GetComponent<MonsterDefinition>()
                    : null;
                if (definition == null ||
                    !Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                    !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                    !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                    !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight))
                {
                    string typeId = definition != null ? definition.TypeId : $"catalog index {index}";
                    throw new InvalidOperationException($"Resident common stats are incorrect: {typeId}");
                }
            }

            Debug.Log($"Resident common stats verification passed. Residents: {catalog.MonsterPrefabs.Count}");
        }

        public static void VerifyAll()
        {
            Verify();
            ManSpriteExperimentSetup.Verify();
            SalesmanSpriteAnimationSetup.Verify();
            PolicemanSpriteAnimationSetup.Verify();
            SamuraiMonsterSetup.Verify();
            WizardMonsterSetup.Verify();
            NinjaMonsterSetup.Verify();
            TurtleMonsterSetup.Verify();
            JeonWoochiMonsterSetup.Verify();
            MascotMonsterSetup.Verify();
            MonsterPrefabSetup.Verify();
            Debug.Log("Resident common stats and feature verification passed.");
        }

        private static MonsterPrefabCatalog LoadCatalog()
        {
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (catalog == null || catalog.MonsterPrefabs.Count == 0)
            {
                throw new InvalidOperationException("Resident prefab catalog is missing or empty.");
            }

            return catalog;
        }
    }
}
