using System;
using IsekaiTruck.Config;
using IsekaiTruck.Monsters;
using IsekaiTruck.Truck;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class SpecialMonsterFoundationVerification
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";

        [MenuItem("Isekai Truck/Verify Special Monster Foundation")]
        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            TextAsset monsterDataFile = AssetDatabase.LoadAssetAtPath<TextAsset>(MonsterDataPath);
            if (config == null || monsterDataFile == null)
            {
                throw new InvalidOperationException("특수 주민 기반 검증용 설정을 불러오지 못했습니다.");
            }

            VerifyDefaultAndCustomContact();
            VerifyManagerContactFlow(config, monsterDataFile);
            VerifyMovementOverride();
            VerifyEnvironmentSpeedMultiplier(config);
            Debug.Log("Special monster foundation verification passed.");
        }

        private static void VerifyManagerContactFlow(GameConfig config, TextAsset monsterDataFile)
        {
            GameObject truckObject = new GameObject("Manager Contact Verification Truck");
            GameObject managerObject = new GameObject("Manager Contact Verification Manager");
            try
            {
                MonsterManager manager = managerObject.AddComponent<MonsterManager>();
                manager.SetDataFile(monsterDataFile);
                manager.Initialize(config, truckObject.transform);

                MonsterController monster = manager.CreateMonster("man", 0f, 0f);
                SpecialMonsterContactProbe contactProbe = monster.gameObject.AddComponent<SpecialMonsterContactProbe>();
                monster.Initialize(monster.Type, truckObject.transform, 0f, config.ReferenceFrameRate);
                manager.UpdateMonsters(1f / config.ReferenceFrameRate);
                if (!contactProbe.WasCalled || manager.Monsters.Count != 1)
                {
                    throw new InvalidOperationException("생존 접촉 결과가 주민 처치와 보상을 막지 못했습니다.");
                }

                Object.DestroyImmediate(contactProbe);
                monster.Initialize(monster.Type, truckObject.transform, 0f, config.ReferenceFrameRate);
                int defeatedCount = 0;
                manager.MonsterDefeated += _ => defeatedCount++;
                manager.UpdateMonsters(1f / config.ReferenceFrameRate);
                if (manager.Monsters.Count != 0 || defeatedCount != 1)
                {
                    throw new InvalidOperationException("일반 주민의 기존 처치 흐름이 유지되지 않았습니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyDefaultAndCustomContact()
        {
            GameObject truckObject = new GameObject("Contact Verification Truck");
            GameObject defaultMonsterObject = new GameObject("Default Contact Verification Monster");
            GameObject specialMonsterObject = new GameObject("Special Contact Verification Monster");
            try
            {
                MonsterData type = CreateTestType();
                MonsterContactContext context = new MonsterContactContext(
                    truckObject.transform,
                    null,
                    1f,
                    1.8f,
                    Vector3.back,
                    Vector3.forward
                );

                MonsterController defaultMonster = defaultMonsterObject.AddComponent<MonsterController>();
                defaultMonster.Initialize(type, truckObject.transform, 0f, 60f);
                if (defaultMonster.ResolveContact(context).Outcome != MonsterContactOutcome.Defeated)
                {
                    throw new InvalidOperationException("일반 주민의 기존 즉시 처치 결과가 유지되지 않았습니다.");
                }

                SpecialMonsterContactProbe contactProbe = specialMonsterObject.AddComponent<SpecialMonsterContactProbe>();
                MonsterController specialMonster = specialMonsterObject.AddComponent<MonsterController>();
                specialMonster.Initialize(type, truckObject.transform, 0f, 60f);
                if (specialMonster.ResolveContact(context).Outcome != MonsterContactOutcome.Survived || !contactProbe.WasCalled)
                {
                    throw new InvalidOperationException("주민별 접촉 결과 확장 지점이 동작하지 않습니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(specialMonsterObject);
                Object.DestroyImmediate(defaultMonsterObject);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyMovementOverride()
        {
            GameObject truckObject = new GameObject("Movement Verification Truck");
            GameObject monsterObject = new GameObject("Movement Verification Monster");
            try
            {
                monsterObject.transform.position = new Vector3(5f, 0f, 0f);
                SpecialMonsterMovementProbe movementProbe = monsterObject.AddComponent<SpecialMonsterMovementProbe>();
                MonsterController monster = monsterObject.AddComponent<MonsterController>();
                monster.Initialize(CreateTestType(), truckObject.transform, 0f, 60f);

                Vector3 startPosition = monsterObject.transform.position;
                monster.UpdateMonster(100f, 0f, 3.096f, 1f, 1f / 60f, 0f, 1f, false);
                if (!movementProbe.WasCalled || monsterObject.transform.position != startPosition)
                {
                    throw new InvalidOperationException("주민별 이동 행동 확장 지점이 기존 이동보다 먼저 처리되지 않습니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(monsterObject);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyEnvironmentSpeedMultiplier(GameConfig config)
        {
            GameObject normalTruckObject = new GameObject("Normal Speed Verification Truck");
            GameObject slowedTruckObject = new GameObject("Environment Speed Verification Truck");
            try
            {
                TruckController normalTruck = normalTruckObject.AddComponent<TruckController>();
                TruckController slowedTruck = slowedTruckObject.AddComponent<TruckController>();
                normalTruck.Initialize(config);
                slowedTruck.Initialize(config);
                normalTruck.SetBlessingMultipliers(1.2f, 1f);
                slowedTruck.SetBlessingMultipliers(1.2f, 1f);
                slowedTruck.SetEnvironmentSpeedMultiplier(0.5f);

                float deltaTime = 1f / config.ReferenceFrameRate;
                for (int frameIndex = 0; frameIndex < 120; frameIndex++)
                {
                    normalTruck.UpdateTruck(Vector2.up, deltaTime);
                    slowedTruck.UpdateTruck(Vector2.up, deltaTime);
                }

                AssertApproximately(slowedTruck.EnvironmentSpeedMultiplier, 0.5f, "환경 속도 배율 상태");
                AssertApproximately(
                    slowedTruck.CurrentSpeed,
                    normalTruck.CurrentSpeed * 0.5f,
                    "환경 현재 속도 감속");
                AssertApproximately(
                    slowedTruckObject.transform.position.z,
                    normalTruckObject.transform.position.z * 0.5f,
                    "환경 실제 이동거리 감속");
                AssertApproximately(
                    slowedTruck.GetStats().MaxSpeed,
                    normalTruck.GetStats().MaxSpeed,
                    "환경 감속과 최대속도 분리");

                slowedTruck.SetEnvironmentSpeedMultiplier(1f);
                AssertApproximately(slowedTruck.CurrentSpeed, normalTruck.CurrentSpeed, "환경 감속 해제");
            }
            finally
            {
                Object.DestroyImmediate(slowedTruckObject);
                Object.DestroyImmediate(normalTruckObject);
            }
        }

        private static MonsterData CreateTestType()
        {
            return new MonsterData(
                "special_test",
                "특수 주민 검증",
                "#FFFFFF",
                Color.white,
                0.6f,
                0.04f,
                7f,
                50,
                2,
                1f
            );
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (!Mathf.Approximately(actual, expected))
            {
                throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {actual}");
            }
        }
    }

    public sealed class SpecialMonsterMovementProbe : MonsterMovementBehavior
    {
        public bool WasCalled { get; private set; }

        protected override bool TryUpdateMovement(MonsterMovementContext context)
        {
            WasCalled = true;
            SetMovementVisual(Vector3.zero, 0f, false);
            return true;
        }
    }

    public sealed class SpecialMonsterContactProbe : MonsterContactBehavior
    {
        public bool WasCalled { get; private set; }

        protected override bool TryResolveContact(MonsterContactContext context, out MonsterContactResult result)
        {
            WasCalled = true;
            result = new MonsterContactResult(MonsterContactOutcome.Survived);
            return true;
        }
    }
}
