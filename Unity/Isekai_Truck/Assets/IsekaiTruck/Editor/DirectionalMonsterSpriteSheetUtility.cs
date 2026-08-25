using System;
using System.Collections.Generic;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    internal static class DirectionalMonsterSpriteSheetUtility
    {
        private const int GridRows = 3;
        private const int SpritesPerRow = 20;
        private const int FramesPerSourceRow = 4;
        private const int MaximumRowGap = 2;
        private const int MaximumSpriteGap = 8;
        private const int SlicePadding = 4;
        private const byte AlphaThreshold = 8;
        private static readonly string[] SourceDirectionNames =
        {
            "Down",
            "DownRight",
            "Right",
            "UpRight",
            "Up"
        };

        public static void ConfigureImporter(string spritePath, string spritePrefix, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Sprite sheet importer was not created: {spritePath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 2048;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (texture == null)
            {
                throw new InvalidOperationException($"Sprite sheet texture could not be loaded: {spritePath}");
            }

            // Keep the current importer path without adding the optional 2D Sprite package.
#pragma warning disable CS0618
            importer.spritesheet = BuildSpriteMetadata(texture, spritePrefix);
#pragma warning restore CS0618
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        public static Sprite[] LoadFrames(string spritePath, string spritePrefix)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            Sprite[] frames = new Sprite[DirectionalSpriteAnimator.ExpectedFrameCount];

            for (int directionIndex = 0; directionIndex < DirectionalSpriteAnimator.SourceDirectionCount; directionIndex++)
            {
                for (int frameIndex = 0; frameIndex < DirectionalSpriteAnimator.DefaultFramesPerDirection; frameIndex++)
                {
                    int flattenedIndex = directionIndex * DirectionalSpriteAnimator.DefaultFramesPerDirection + frameIndex;
                    string spriteName = GetSpriteName(spritePrefix, directionIndex, frameIndex);

                    for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                    {
                        Sprite sprite = assets[assetIndex] as Sprite;
                        if (sprite != null && sprite.name == spriteName)
                        {
                            frames[flattenedIndex] = sprite;
                            break;
                        }
                    }

                    if (frames[flattenedIndex] == null)
                    {
                        throw new InvalidOperationException($"Directional sprite frame is missing: {spriteName}");
                    }
                }
            }

            return frames;
        }

        private static SpriteMetaData[] BuildSpriteMetadata(Texture2D texture, string spritePrefix)
        {
            Color32[] pixels = texture.GetPixels32();
            SpriteMetaData[] metadata = new SpriteMetaData[DirectionalSpriteAnimator.ExpectedFrameCount];
            List<Vector2Int> sourceRows = FindOccupiedRows(pixels, texture.width, texture.height);
            if (sourceRows.Count != GridRows)
            {
                throw new InvalidOperationException($"Expected {GridRows} sprite rows, but found {sourceRows.Count}.");
            }

            List<Vector2Int>[] sourceColumns = new List<Vector2Int>[GridRows];
            for (int rowIndex = 0; rowIndex < GridRows; rowIndex++)
            {
                sourceColumns[rowIndex] = FindOccupiedColumns(pixels, texture.width, sourceRows[rowIndex]);
                if (sourceColumns[rowIndex].Count != SpritesPerRow)
                {
                    throw new InvalidOperationException(
                        $"Expected {SpritesPerRow} sprites in row {rowIndex}, but found {sourceColumns[rowIndex].Count}."
                    );
                }
            }

            for (int directionIndex = 0; directionIndex < DirectionalSpriteAnimator.SourceDirectionCount; directionIndex++)
            {
                for (int frameIndex = 0; frameIndex < DirectionalSpriteAnimator.DefaultFramesPerDirection; frameIndex++)
                {
                    int rowFromTop = frameIndex / FramesPerSourceRow;
                    int sourceRowIndex = GridRows - rowFromTop - 1;
                    int groupColumn = frameIndex % FramesPerSourceRow;
                    int sourceColumn = groupColumn * DirectionalSpriteAnimator.SourceDirectionCount + directionIndex;
                    Vector2Int rowBounds = sourceRows[sourceRowIndex];
                    Vector2Int columnBounds = sourceColumns[sourceRowIndex][sourceColumn];
                    RectInt spriteRect = ExpandBounds(rowBounds, columnBounds, texture.width, texture.height);
                    int flattenedIndex = directionIndex * DirectionalSpriteAnimator.DefaultFramesPerDirection + frameIndex;
                    float pivotY = Mathf.Min(SlicePadding, rowBounds.x) / (float)spriteRect.height;

                    metadata[flattenedIndex] = new SpriteMetaData
                    {
                        name = GetSpriteName(spritePrefix, directionIndex, frameIndex),
                        rect = new Rect(spriteRect.x, spriteRect.y, spriteRect.width, spriteRect.height),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, pivotY),
                        border = Vector4.zero
                    };
                }
            }

            return metadata;
        }

        private static List<Vector2Int> FindOccupiedRows(Color32[] pixels, int textureWidth, int textureHeight)
        {
            bool[] occupiedRows = new bool[textureHeight];
            for (int y = 0; y < textureHeight; y++)
            {
                int rowOffset = y * textureWidth;
                for (int x = 0; x < textureWidth; x++)
                {
                    if (pixels[rowOffset + x].a <= AlphaThreshold)
                    {
                        continue;
                    }

                    occupiedRows[y] = true;
                    break;
                }
            }

            return FindOccupiedRuns(occupiedRows, MaximumRowGap);
        }

        private static List<Vector2Int> FindOccupiedColumns(Color32[] pixels, int textureWidth, Vector2Int rowBounds)
        {
            bool[] occupiedColumns = new bool[textureWidth];

            for (int y = rowBounds.x; y <= rowBounds.y; y++)
            {
                int rowOffset = y * textureWidth;
                for (int x = 0; x < textureWidth; x++)
                {
                    if (pixels[rowOffset + x].a > AlphaThreshold)
                    {
                        occupiedColumns[x] = true;
                    }
                }
            }

            return FindOccupiedRuns(occupiedColumns, MaximumSpriteGap);
        }

        private static List<Vector2Int> FindOccupiedRuns(bool[] occupied, int maximumGap)
        {
            List<Vector2Int> runs = new List<Vector2Int>();
            int runStart = -1;
            int lastOccupied = -1;

            for (int index = 0; index < occupied.Length; index++)
            {
                if (!occupied[index])
                {
                    continue;
                }

                if (runStart < 0)
                {
                    runStart = index;
                    lastOccupied = index;
                    continue;
                }

                if (index - lastOccupied - 1 <= maximumGap)
                {
                    lastOccupied = index;
                    continue;
                }

                runs.Add(new Vector2Int(runStart, lastOccupied));
                runStart = index;
                lastOccupied = index;
            }

            if (runStart >= 0)
            {
                runs.Add(new Vector2Int(runStart, lastOccupied));
            }

            return runs;
        }

        private static RectInt ExpandBounds(Vector2Int rowBounds, Vector2Int columnBounds, int textureWidth, int textureHeight)
        {
            int paddedMinX = Mathf.Max(0, columnBounds.x - SlicePadding);
            int paddedMinY = Mathf.Max(0, rowBounds.x - SlicePadding);
            int paddedMaxX = Mathf.Min(textureWidth - 1, columnBounds.y + SlicePadding);
            int paddedMaxY = Mathf.Min(textureHeight - 1, rowBounds.y + SlicePadding);
            return new RectInt(
                paddedMinX,
                paddedMinY,
                paddedMaxX - paddedMinX + 1,
                paddedMaxY - paddedMinY + 1
            );
        }

        private static string GetSpriteName(string spritePrefix, int directionIndex, int frameIndex)
        {
            return $"{spritePrefix}_{SourceDirectionNames[directionIndex]}_{frameIndex}";
        }
    }
}
