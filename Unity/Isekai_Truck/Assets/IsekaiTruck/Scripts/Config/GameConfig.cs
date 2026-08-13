using System;
using UnityEngine;

namespace IsekaiTruck.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Isekai Truck/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float referenceFrameRate = 60f;
        [SerializeField] private TruckSettings truck = new TruckSettings();
        [SerializeField] private WorldSettings world = new WorldSettings();
        [SerializeField] private CameraSettings cameraSettings = new CameraSettings();
        [SerializeField] private MonsterSettings monster = new MonsterSettings();
        [SerializeField] private SpawnSettings spawn = new SpawnSettings();
        [SerializeField] private PlayerSettings player = new PlayerSettings();

        public float ReferenceFrameRate => referenceFrameRate;
        public TruckSettings Truck => truck;
        public WorldSettings World => world;
        public CameraSettings Camera => cameraSettings;
        public MonsterSettings Monster => monster;
        public SpawnSettings Spawn => spawn;
        public PlayerSettings Player => player;

        [Serializable]
        public sealed class TruckSettings
        {
            [SerializeField, Min(0f)] private float baseMaxSpeed = 0.1f;
            [SerializeField, Min(0f)] private float acceleration = 0.001f;
            [SerializeField, Range(0f, 1f)] private float friction = 0.94f;
            [SerializeField, Range(0f, 1f)] private float turnSpeed = 0.03f;
            [SerializeField, Min(0f)] private float speedPerUpgrade = 0.01f;
            [SerializeField, Min(0f)] private float sizePerUpgrade = 0.1f;

            public float BaseMaxSpeed => baseMaxSpeed;
            public float Acceleration => acceleration;
            public float Friction => friction;
            public float TurnSpeed => turnSpeed;
            public float SpeedPerUpgrade => speedPerUpgrade;
            public float SizePerUpgrade => sizePerUpgrade;
        }

        [Serializable]
        public sealed class WorldSettings
        {
            [SerializeField, Min(1f)] private float tileSize = 50f;
            [SerializeField, Min(0)] private int baseTileRadius = 2;
            [SerializeField, Min(0)] private int maxTileRadius = 18;
            [SerializeField] private Color fogColor = new Color32(0x87, 0xce, 0xeb, 0xff);
            [SerializeField, Min(0f)] private float baseFogNear = 55f;
            [SerializeField, Min(0f)] private float baseFogFar = 90f;
            [SerializeField, Min(0f)] private float fogScaleStrength = 0.7f;
            [SerializeField] private Color groundColor = new Color32(0x3a, 0x7a, 0x2a, 0xff);
            [SerializeField] private Color groundPatternColor = new Color32(0x2f, 0x66, 0x22, 0xff);
            [SerializeField, Min(0.1f)] private float groundPatternSize = 5f;

            public float TileSize => tileSize;
            public int BaseTileRadius => baseTileRadius;
            public int MaxTileRadius => maxTileRadius;
            public Color FogColor => fogColor;
            public float BaseFogNear => baseFogNear;
            public float BaseFogFar => baseFogFar;
            public float FogScaleStrength => fogScaleStrength;
            public Color GroundColor => groundColor;
            public Color GroundPatternColor => groundPatternColor;
            public float GroundPatternSize => groundPatternSize;
        }

        [Serializable]
        public sealed class CameraSettings
        {
            [SerializeField] private Vector3 offset = new Vector3(0f, 18f, 12f);
            [SerializeField] private Vector3 lookTarget = new Vector3(0f, 4f, -2f);
            [SerializeField, Range(0f, 1f)] private float followSpeed = 0.08f;
            [SerializeField, Min(0f)] private float zoomStartScale = 1.2f;
            [SerializeField, Min(0f)] private float zoomStrength = 0.8f;
            [SerializeField, Min(1f)] private float maxZoomMultiplier = 10f;
            [SerializeField] private Vector2Int viewportAspect = new Vector2Int(10, 16);
            [SerializeField, Range(1f, 179f)] private float fieldOfView = 75f;
            [SerializeField, Min(0.001f)] private float nearClipPlane = 0.1f;
            [SerializeField, Min(0.01f)] private float farClipPlane = 1000f;

            public Vector3 Offset => offset;
            public Vector3 LookTarget => lookTarget;
            public float FollowSpeed => followSpeed;
            public float ZoomStartScale => zoomStartScale;
            public float ZoomStrength => zoomStrength;
            public float MaxZoomMultiplier => maxZoomMultiplier;
            public float ViewportAspect => viewportAspect.y > 0 ? (float)viewportAspect.x / viewportAspect.y : 1f;
            public float FieldOfView => fieldOfView;
            public float NearClipPlane => nearClipPlane;
            public float FarClipPlane => farClipPlane;
        }

        [Serializable]
        public sealed class MonsterSettings
        {
            [SerializeField, Min(0f)] private float collisionDistance = 1.8f;
            [SerializeField, Min(0f)] private float directionLockMultiplier = 1.72f;

            public float CollisionDistance => collisionDistance;
            public float DirectionLockMultiplier => directionLockMultiplier;
        }

        [Serializable]
        public sealed class SpawnSettings
        {
            [SerializeField, Min(0)] private int targetCount = 100;
            [SerializeField, Min(0f)] private float minDistance = 35f;
            [SerializeField, Min(0f)] private float maxDistance = 70f;
            [SerializeField, Min(0f)] private float despawnDistance = 80f;
            [SerializeField, Min(0)] private int spawnIntervalMilliseconds = 10;
            [SerializeField, Min(1)] private int spawnPerInterval = 1;

            public int TargetCount => targetCount;
            public float MinDistance => minDistance;
            public float MaxDistance => maxDistance;
            public float DespawnDistance => despawnDistance;
            public int SpawnIntervalMilliseconds => spawnIntervalMilliseconds;
            public int SpawnPerInterval => spawnPerInterval;
        }

        [Serializable]
        public sealed class PlayerSettings
        {
            [SerializeField, Min(1)] private int startLevel = 1;
            [SerializeField, Min(0)] private int startExp;
            [SerializeField, Min(0)] private int startSoul;
            [SerializeField, Min(1)] private int baseRequiredExp = 100;
            [SerializeField, Min(0f)] private float expGrowth = 1.5f;
            [SerializeField, Min(0)] private int upgradePointPerLevel = 1;

            public int StartLevel => startLevel;
            public int StartExp => startExp;
            public int StartSoul => startSoul;
            public int BaseRequiredExp => baseRequiredExp;
            public float ExpGrowth => expGrowth;
            public int UpgradePointPerLevel => upgradePointPerLevel;
        }
    }
}
