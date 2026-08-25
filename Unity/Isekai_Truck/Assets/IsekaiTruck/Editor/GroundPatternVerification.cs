using System;
using IsekaiTruck.Config;
using IsekaiTruck.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class GroundPatternVerification
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException("바닥 패턴 검증용 GameConfig를 불러오지 못했습니다.");
            }

            GameObject playerObject = new GameObject("Ground Pattern Verification Player");
            GameObject cameraObject = new GameObject("Ground Pattern Verification Camera");
            GameObject worldObject = new GameObject("Ground Pattern Verification World");

            try
            {
                UnityEngine.Camera targetCamera = cameraObject.AddComponent<UnityEngine.Camera>();
                WorldManager worldManager = worldObject.AddComponent<WorldManager>();
                worldManager.Initialize(config, playerObject.transform, targetCamera);

                int expectedTileCount = (config.World.BaseTileRadius * 2 + 1) * (config.World.BaseTileRadius * 2 + 1);
                if (worldObject.transform.childCount != expectedTileCount)
                {
                    throw new InvalidOperationException(
                        $"바닥 타일 수 검증 실패: expected {expectedTileCount}, actual {worldObject.transform.childCount}"
                    );
                }

                MeshRenderer tileRenderer = worldObject.transform.GetChild(0).GetComponent<MeshRenderer>();
                Material material = tileRenderer.sharedMaterial;
                Texture2D texture = material.mainTexture as Texture2D;

                if (texture == null || texture.filterMode != FilterMode.Point || texture.wrapMode != TextureWrapMode.Repeat)
                {
                    throw new InvalidOperationException("바닥 반복 패턴 텍스처 설정 검증에 실패했습니다.");
                }

                Color[] colors = texture.GetPixels();
                AssertColor(colors[0], config.World.GroundColor, "기본 바닥색");
                AssertColor(colors[1], config.World.GroundPatternColor, "바닥 패턴색");
                AssertColor(colors[2], config.World.GroundPatternColor, "대각 패턴색");
                AssertColor(colors[3], config.World.GroundColor, "대각 기본색");

                float expectedRepeat = Mathf.Max(
                    1f,
                    Mathf.Round(config.World.TileSize / (Mathf.Max(config.World.GroundPatternSize, 0.1f) * 2f))
                );
                AssertApproximately(material.mainTextureScale.x, expectedRepeat, "바닥 패턴 반복 수");
                AssertApproximately(material.mainTextureScale.y, expectedRepeat, "바닥 패턴 반복 수");
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
            }

            Debug.Log("Ground pattern verification passed.");
        }

        private static void AssertColor(Color actual, Color expected, string label)
        {
            if (Mathf.Abs(actual.r - expected.r) <= 0.001f &&
                Mathf.Abs(actual.g - expected.g) <= 0.001f &&
                Mathf.Abs(actual.b - expected.b) <= 0.001f &&
                Mathf.Abs(actual.a - expected.a) <= 0.001f)
            {
                return;
            }

            throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {actual}");
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) <= 0.0001f)
            {
                return;
            }

            throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {actual}");
        }
    }
}
