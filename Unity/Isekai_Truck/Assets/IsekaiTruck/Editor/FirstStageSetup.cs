using System;
using System.Collections.Generic;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class FirstStageSetup
    {
        private const string RootFolder = "Assets/IsekaiTruck";
        private const string ConfigFolder = RootFolder + "/Config";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string ConfigPath = ConfigFolder + "/GameConfig.asset";
        private const string TruckMaterialPath = MaterialFolder + "/Truck.mat";
        private const string ScenePath = SceneFolder + "/Main.unity";

        [MenuItem("Isekai Truck/Setup First Migration Stage")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            EnsureFolder(ConfigFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(SceneFolder);

            GameConfig config = GetOrCreateConfig();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);

            if (config == null)
            {
                throw new InvalidOperationException($"GameConfig를 불러오지 못했습니다: {ConfigPath}");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Scene existingScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                RepairConfigReference(config);
                EditorSceneManager.MarkSceneDirty(existingScene);
                EditorSceneManager.SaveScene(existingScene, ScenePath);
                AddSceneToBuildSettings();
                AssetDatabase.SaveAssets();
                ValidateConfigReference();

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Isekai Truck", "기존 Main 씬을 열었습니다.", "확인");
                }

                return;
            }

            Material truckMaterial = GetOrCreateTruckMaterial();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject gameManagerObject = new GameObject("GameManager");
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();

            GameObject worldObject = new GameObject("World");
            WorldManager worldManager = worldObject.AddComponent<WorldManager>();

            GameObject truck = new GameObject("Truck");
            truck.transform.position = new Vector3(0f, 0.5f, 0f);

            GameObject truckVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            truckVisual.name = "Visual";
            truckVisual.transform.SetParent(truck.transform, false);
            truckVisual.transform.localScale = new Vector3(1.5f, 1f, 3f);
            Object.DestroyImmediate(truckVisual.GetComponent<BoxCollider>());
            truckVisual.GetComponent<MeshRenderer>().sharedMaterial = truckMaterial;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            UnityEngine.Camera targetCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            cameraObject.AddComponent<AudioListener>();
            CameraController cameraController = cameraObject.AddComponent<CameraController>();

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.color = Color.white;
            directionalLight.intensity = 1f;
            directionalLight.shadows = LightShadows.None;

            RenderSettings.skybox = null;

            SerializedObject cameraControllerObject = new SerializedObject(cameraController);
            cameraControllerObject.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            cameraControllerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject gameManagerSerializedObject = new SerializedObject(gameManager);
            gameManagerSerializedObject.FindProperty("playerTarget").objectReferenceValue = truck.transform;
            gameManagerSerializedObject.FindProperty("cameraController").objectReferenceValue = cameraController;
            gameManagerSerializedObject.FindProperty("worldManager").objectReferenceValue = worldManager;
            gameManagerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            gameManager.SetConfig(config);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = gameManagerObject;

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "Main 씬과 GameConfig가 생성되었습니다. Play를 눌러 월드와 카메라를 확인할 수 있습니다.",
                    "확인"
                );
            }
        }

        private static GameConfig GetOrCreateConfig()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static void RepairConfigReference(GameConfig config)
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                return;
            }

            gameManager.SetConfig(config);
            EditorUtility.SetDirty(gameManager);
        }

        private static void ValidateConfigReference()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                throw new InvalidOperationException("Main 씬에서 GameManager를 찾지 못했습니다.");
            }

            SerializedObject gameManagerSerializedObject = new SerializedObject(gameManager);
            gameManagerSerializedObject.Update();

            if (gameManagerSerializedObject.FindProperty("config").objectReferenceValue == null)
            {
                throw new InvalidOperationException("GameManager에 GameConfig 참조를 저장하지 못했습니다.");
            }
        }

        private static Material GetOrCreateTruckMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(TruckMaterialPath);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                color = new Color32(0x33, 0x66, 0xff, 0xff)
            };

            AssetDatabase.CreateAsset(material, TruckMaterialPath);
            return material;
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != ScenePath) continue;

                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int separatorIndex = path.LastIndexOf('/');
            string parent = path.Substring(0, separatorIndex);
            string folderName = path.Substring(separatorIndex + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
