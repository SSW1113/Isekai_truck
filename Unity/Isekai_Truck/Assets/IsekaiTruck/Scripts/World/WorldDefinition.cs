using System.Collections.Generic;
using UnityEngine;

namespace IsekaiTruck.World
{
    [CreateAssetMenu(fileName = "WorldDefinition", menuName = "Isekai Truck/World Definition")]
    public sealed class WorldDefinition : ScriptableObject
    {
        [SerializeField] private string id = "world";
        [SerializeField] private string displayName = "세계";
        [SerializeField] private Color skyColor = new Color32(0x87, 0xce, 0xeb, 0xff);
        [SerializeField] private Color fogColor = new Color32(0x87, 0xce, 0xeb, 0xff);
        [SerializeField] private Color groundColor = new Color32(0x3a, 0x7a, 0x2a, 0xff);
        [SerializeField] private Color groundPatternColor = new Color32(0x2f, 0x66, 0x22, 0xff);
        [SerializeField] private ModernCityChunkPrototype[] chunkPrefabs = new ModernCityChunkPrototype[0];
        [SerializeField, Min(1)] private int crossroadInterval = 4;

        public string Id => id;
        public string DisplayName => displayName;
        public Color SkyColor => skyColor;
        public Color FogColor => fogColor;
        public Color GroundColor => groundColor;
        public Color GroundPatternColor => groundPatternColor;
        public IReadOnlyList<ModernCityChunkPrototype> ChunkPrefabs => chunkPrefabs;
        public int CrossroadInterval => crossroadInterval;

#if UNITY_EDITOR
        public void SetEditorValues(
            string worldId,
            string worldDisplayName,
            Color worldSkyColor,
            Color worldFogColor,
            Color worldGroundColor,
            Color worldGroundPatternColor
        )
        {
            id = worldId;
            displayName = worldDisplayName;
            skyColor = worldSkyColor;
            fogColor = worldFogColor;
            groundColor = worldGroundColor;
            groundPatternColor = worldGroundPatternColor;
        }

        public void SetEditorChunkLayout(ModernCityChunkPrototype[] prefabs, int interval)
        {
            chunkPrefabs = prefabs ?? new ModernCityChunkPrototype[0];
            crossroadInterval = Mathf.Max(1, interval);
        }
#endif
    }
}
