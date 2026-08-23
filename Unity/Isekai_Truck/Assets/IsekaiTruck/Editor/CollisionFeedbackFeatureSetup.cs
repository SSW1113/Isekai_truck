using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Monsters;
using IsekaiTruck.Truck;
using IsekaiTruck.UI;
using IsekaiTruck.Visuals;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class CollisionFeedbackFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";
        private const string PrefabFolder = "Assets/IsekaiTruck/Prefabs/Effects";
        private const string ImpactPrefabPath = PrefabFolder + "/CartoonImpactBurst.prefab";
        private const int SoulOrbPoolSize = 12;

        [MenuItem("Isekai Truck/Setup Collision Feedback")]
        public static void Setup()
        {
            EnsureFolder(PrefabFolder);
            ParticleSystem impactPrefab = CreateImpactPrefab();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            CameraController cameraController = Object.FindFirstObjectByType<CameraController>();
            TruckSpriteView truckSpriteView = Object.FindFirstObjectByType<TruckSpriteView>();
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            Canvas gameCanvas = GameObject.Find("Game Canvas")?.GetComponent<Canvas>();
            if (gameManager == null || cameraController == null || truckSpriteView == null || gameUI == null || gameCanvas == null)
            {
                throw new InvalidOperationException("Collision feedback scene dependencies were not found.");
            }

            Transform effectRoot = GetOrCreateSceneObject("Collision Effects").transform;
            CollisionFeedbackController collisionFeedback = GetOrCreateSceneComponent<CollisionFeedbackController>("Collision Feedback");
            collisionFeedback.SetReferences(truckSpriteView, impactPrefab, effectRoot);

            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            TMP_Text soulText = (TMP_Text)serializedGameUI.FindProperty("soulText").objectReferenceValue;
            RectTransform soulTarget = soulText != null ? soulText.rectTransform.parent as RectTransform : null;
            if (soulTarget == null)
            {
                throw new InvalidOperationException("Soul UI target was not found.");
            }

            SoulRewardFlyUI soulRewardUI = CreateSoulRewardUI(gameCanvas, gameUI, soulTarget);
            gameManager.SetCollisionFeedbackSystems(collisionFeedback, soulRewardUI);
            EditorUtility.SetDirty(collisionFeedback);
            EditorUtility.SetDirty(soulRewardUI);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "충돌 타격감과 영혼 흡수 연출을 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            ParticleSystem impactPrefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(ImpactPrefabPath);
            if (impactPrefab == null || impactPrefab.GetComponentsInChildren<ParticleSystem>(true).Length < 2)
            {
                throw new InvalidOperationException("Cartoon impact prefab is incomplete.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            CollisionFeedbackController collisionFeedback = Object.FindFirstObjectByType<CollisionFeedbackController>();
            SoulRewardFlyUI soulRewardUI = Object.FindFirstObjectByType<SoulRewardFlyUI>(FindObjectsInactive.Include);
            if (gameManager == null || collisionFeedback == null || soulRewardUI == null)
            {
                throw new InvalidOperationException("Collision feedback scene systems are missing.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            if (serializedGameManager.FindProperty("collisionFeedbackController").objectReferenceValue != collisionFeedback
                || serializedGameManager.FindProperty("soulRewardFlyUI").objectReferenceValue != soulRewardUI)
            {
                throw new InvalidOperationException("GameManager collision feedback references are incomplete.");
            }

            SerializedObject serializedSoulUI = new SerializedObject(soulRewardUI);
            if (serializedSoulUI.FindProperty("soulTarget").objectReferenceValue == null
                || serializedSoulUI.FindProperty("orbTemplate").objectReferenceValue == null
                || serializedSoulUI.FindProperty("poolSize").intValue != SoulOrbPoolSize)
            {
                throw new InvalidOperationException("Soul reward UI pool is incomplete.");
            }

            VerifyFeedbackSignals();
            Debug.Log("Collision feedback verification passed.");
        }

        private static void VerifyFeedbackSignals()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            TextAsset monsterData = AssetDatabase.LoadAssetAtPath<TextAsset>(MonsterDataPath);
            GameObject truck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject monsterManagerObject = new GameObject("Feedback Signal Monster Manager");

            try
            {
                TruckDamageFlash flash = truck.AddComponent<TruckDamageFlash>();
                TruckHealthController health = truck.AddComponent<TruckHealthController>();
                health.Initialize(config, flash);
                int damageEventCount = 0;
                health.DamageTaken += result =>
                {
                    if (result.AppliedDamage == 1)
                    {
                        damageEventCount++;
                    }
                };

                if (!health.TryTakeDamage(1) || health.TryTakeDamage(1) || damageEventCount != 1)
                {
                    throw new InvalidOperationException("Truck damage feedback signal did not match actual health loss.");
                }

                MonsterManager manager = monsterManagerObject.AddComponent<MonsterManager>();
                manager.SetDataFile(monsterData);
                manager.Initialize(config, truck.transform);
                int detailedEventCount = 0;
                int batchCount = 0;
                manager.MonsterDefeatedDetailed += context => detailedEventCount++;
                manager.MonsterCollisionBatchCompleted += batch => batchCount += batch.Count;
                manager.CreateMonster("man", truck.transform.position.x, truck.transform.position.z);
                manager.UpdateMonsters(0f);

                if (detailedEventCount != 1 || batchCount != 1)
                {
                    throw new InvalidOperationException("Monster collision feedback signals were not emitted once per defeat and batch.");
                }
            }
            finally
            {
                Object.DestroyImmediate(monsterManagerObject);
                Object.DestroyImmediate(truck);
            }
        }

        private static ParticleSystem CreateImpactPrefab()
        {
            GameObject root = new GameObject("CartoonImpactBurst");
            try
            {
                ParticleSystem sparks = root.AddComponent<ParticleSystem>();
                ConfigureSparks(sparks);

                GameObject poofObject = new GameObject("Poof");
                poofObject.transform.SetParent(root.transform, false);
                ParticleSystem poof = poofObject.AddComponent<ParticleSystem>();
                ConfigurePoof(poof);

                Material particleMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
                sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
                poof.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;

                PrefabUtility.SaveAsPrefabAsset(root, ImpactPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<ParticleSystem>(ImpactPrefabPath);
        }

        private static void ConfigureSparks(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.32f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.94f, 0.48f, 1f),
                new Color(1f, 0.64f, 0.2f, 1f)
            );
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            shape.randomDirectionAmount = 1f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = CreateFadeGradient(new Color(1f, 0.86f, 0.28f, 1f));

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 1.6f;
            renderer.sortingOrder = 8;
        }

        private static void ConfigurePoof(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.94f, 0.88f, 0.98f, 0.85f),
                new Color(0.72f, 0.66f, 0.78f, 0.72f)
            );
            main.maxParticles = 18;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.22f, 7) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.18f;
            shape.randomDirectionAmount = 0.8f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = CreateFadeGradient(new Color(0.86f, 0.8f, 0.9f, 0.82f));

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.35f));

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 7;
        }

        private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color color)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(color.a, 0f),
                    new GradientAlphaKey(color.a, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        private static SoulRewardFlyUI CreateSoulRewardUI(Canvas canvas, GameUIController gameUI, RectTransform soulTarget)
        {
            Transform existing = canvas.transform.Find("Soul Reward FX");
            GameObject rootObject;
            if (existing == null)
            {
                rootObject = new GameObject("Soul Reward FX", typeof(RectTransform));
                rootObject.transform.SetParent(canvas.transform, false);
            }
            else
            {
                rootObject = existing.gameObject;
                for (int i = existing.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(existing.GetChild(i).gameObject);
                }
            }

            RectTransform root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.SetAsLastSibling();

            SoulRewardFlyUI controller = rootObject.GetComponent<SoulRewardFlyUI>();
            if (controller == null)
            {
                controller = rootObject.AddComponent<SoulRewardFlyUI>();
            }

            Sprite orbSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            GameObject orbObject = new GameObject("Soul Orb Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform orb = orbObject.GetComponent<RectTransform>();
            orb.SetParent(root, false);
            orb.anchorMin = new Vector2(0.5f, 0.5f);
            orb.anchorMax = new Vector2(0.5f, 0.5f);
            orb.pivot = new Vector2(0.5f, 0.5f);
            orb.sizeDelta = new Vector2(26f, 26f);
            orb.localScale = Vector3.zero;

            Image orbImage = orbObject.GetComponent<Image>();
            orbImage.sprite = orbSprite;
            orbImage.color = new Color(1f, 0.9f, 0.5f, 1f);
            orbImage.raycastTarget = false;
            orbImage.enabled = false;

            controller.SetReferences(canvas, root, soulTarget, orbImage, gameUI);
            return controller;
        }

        private static GameObject GetOrCreateSceneObject(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            return existing != null ? existing : new GameObject(objectName);
        }

        private static T GetOrCreateSceneComponent<T>(string objectName) where T : Component
        {
            T existing = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            return GetOrCreateSceneObject(objectName).AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
