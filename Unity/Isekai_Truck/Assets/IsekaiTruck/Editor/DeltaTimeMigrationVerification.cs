using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Monsters;
using IsekaiTruck.Truck;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace IsekaiTruck.Editor
{
    public static class DeltaTimeMigrationVerification
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            TextAsset monsterDataFile = AssetDatabase.LoadAssetAtPath<TextAsset>(MonsterDataPath);

            if (config == null || monsterDataFile == null)
            {
                throw new InvalidOperationException("deltaTime 검증용 설정 또는 몬스터 데이터를 불러오지 못했습니다.");
            }

            AssertApproximately(config.ReferenceFrameRate, 60f, 0.0001f, "기준 FPS");

            TruckSimulation truck30 = RunTruckSimulation(config, 30);
            TruckSimulation truck60 = RunTruckSimulation(config, 60);
            TruckSimulation truck120 = RunTruckSimulation(config, 120);

            AssertApproximately(truck30.Speed, truck60.Speed, 0.0001f, "30/60 FPS 트럭 속도");
            AssertApproximately(truck120.Speed, truck60.Speed, 0.0001f, "120/60 FPS 트럭 속도");
            AssertApproximately(truck30.ReleasedSpeed, truck60.ReleasedSpeed, 0.0001f, "30/60 FPS 마찰");
            AssertApproximately(truck120.ReleasedSpeed, truck60.ReleasedSpeed, 0.0001f, "120/60 FPS 마찰");
            AssertApproximately(truck30.TurnAngle, truck60.TurnAngle, 0.001f, "30/60 FPS 회전");
            AssertApproximately(truck120.TurnAngle, truck60.TurnAngle, 0.001f, "120/60 FPS 회전");
            AssertApproximately(truck30.Distance, truck60.Distance, 0.04f, "30/60 FPS 가속 이동거리");
            AssertApproximately(truck120.Distance, truck60.Distance, 0.04f, "120/60 FPS 가속 이동거리");
            AssertApproximately(truck30.ReleasedDistance, truck60.ReleasedDistance, 0.001f, "30/60 FPS 관성 이동거리");
            AssertApproximately(truck120.ReleasedDistance, truck60.ReleasedDistance, 0.001f, "120/60 FPS 관성 이동거리");

            float monster30 = RunMonsterSimulation(config, monsterDataFile, 30);
            float monster60 = RunMonsterSimulation(config, monsterDataFile, 60);
            float monster120 = RunMonsterSimulation(config, monsterDataFile, 120);
            AssertApproximately(monster30, monster60, 0.001f, "30/60 FPS 몬스터 이동");
            AssertApproximately(monster120, monster60, 0.001f, "120/60 FPS 몬스터 이동");

            float camera30 = RunCameraSimulation(config, 30);
            float camera60 = RunCameraSimulation(config, 60);
            float camera120 = RunCameraSimulation(config, 120);
            AssertApproximately(camera30, camera60, 0.001f, "30/60 FPS 카메라 추적");
            AssertApproximately(camera120, camera60, 0.001f, "120/60 FPS 카메라 추적");

            Debug.Log("DeltaTime migration verification passed.");
        }

        private static TruckSimulation RunTruckSimulation(GameConfig config, int framesPerSecond)
        {
            float deltaTime = 1f / framesPerSecond;
            GameObject straightObject = new GameObject($"Truck {framesPerSecond} FPS Straight");
            GameObject turnObject = new GameObject($"Truck {framesPerSecond} FPS Turn");

            try
            {
                TruckController straightController = straightObject.AddComponent<TruckController>();
                straightController.Initialize(config);

                for (int i = 0; i < framesPerSecond; i++)
                {
                    straightController.UpdateTruck(new Vector2(0f, 1f), deltaTime);
                }

                float speed = straightController.CurrentSpeed;
                float distance = straightObject.transform.position.z;

                for (int i = 0; i < framesPerSecond / 2; i++)
                {
                    straightController.UpdateTruck(Vector2.zero, deltaTime);
                }

                float releasedDistance = straightObject.transform.position.z - distance;

                TruckController turnController = turnObject.AddComponent<TruckController>();
                turnController.Initialize(config);

                for (int i = 0; i < framesPerSecond; i++)
                {
                    turnController.UpdateTruck(new Vector2(1f, 0f), deltaTime);
                }

                float turnAngle = Mathf.DeltaAngle(0f, turnObject.transform.eulerAngles.y);
                return new TruckSimulation(speed, distance, straightController.CurrentSpeed, releasedDistance, turnAngle);
            }
            finally
            {
                Object.DestroyImmediate(turnObject);
                Object.DestroyImmediate(straightObject);
            }
        }

        private static float RunMonsterSimulation(GameConfig config, TextAsset monsterDataFile, int framesPerSecond)
        {
            GameObject truckObject = new GameObject($"Monster {framesPerSecond} FPS Truck");
            GameObject managerObject = new GameObject($"Monster {framesPerSecond} FPS Manager");
            Random.State randomState = Random.state;

            try
            {
                Random.InitState(20260813);
                MonsterManager manager = managerObject.AddComponent<MonsterManager>();
                manager.SetDataFile(monsterDataFile);
                manager.Initialize(config, truckObject.transform);

                MonsterController monster = manager.CreateMonster("man", 100f, 0f);
                Vector3 startPosition = monster.transform.position;
                float deltaTime = 1f / framesPerSecond;

                for (int i = 0; i < framesPerSecond; i++)
                {
                    manager.UpdateMonsters(deltaTime);
                }

                return Vector3.Distance(startPosition, monster.transform.position);
            }
            finally
            {
                Random.state = randomState;
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static float RunCameraSimulation(GameConfig config, int framesPerSecond)
        {
            GameObject targetObject = new GameObject($"Camera {framesPerSecond} FPS Target");
            GameObject cameraObject = new GameObject($"Camera {framesPerSecond} FPS Camera");

            try
            {
                targetObject.transform.position = new Vector3(10f, 0f, 0f);
                cameraObject.AddComponent<UnityEngine.Camera>();
                CameraController controller = cameraObject.AddComponent<CameraController>();
                controller.Initialize(config, targetObject.transform);

                float deltaTime = 1f / framesPerSecond;
                for (int i = 0; i < framesPerSecond; i++)
                {
                    controller.UpdateCamera(deltaTime);
                }

                return cameraObject.transform.position.x;
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        private static void AssertApproximately(float actual, float expected, float tolerance, string label)
        {
            if (Mathf.Abs(actual - expected) <= tolerance)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{label} 검증 실패: expected {expected}, actual {actual}, tolerance {tolerance}"
            );
        }

        private readonly struct TruckSimulation
        {
            public TruckSimulation(float speed, float distance, float releasedSpeed, float releasedDistance, float turnAngle)
            {
                Speed = speed;
                Distance = distance;
                ReleasedSpeed = releasedSpeed;
                ReleasedDistance = releasedDistance;
                TurnAngle = turnAngle;
            }

            public float Speed { get; }
            public float Distance { get; }
            public float ReleasedSpeed { get; }
            public float ReleasedDistance { get; }
            public float TurnAngle { get; }
        }
    }
}
