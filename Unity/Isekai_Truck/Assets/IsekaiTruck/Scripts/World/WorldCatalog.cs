using System.Collections.Generic;
using UnityEngine;

namespace IsekaiTruck.World
{
    [CreateAssetMenu(fileName = "WorldCatalog", menuName = "Isekai Truck/World Catalog")]
    public sealed class WorldCatalog : ScriptableObject
    {
        [SerializeField] private List<WorldDefinition> worlds = new List<WorldDefinition>();

        public IReadOnlyList<WorldDefinition> Worlds => worlds;

        public WorldDefinition FindById(string worldId)
        {
            if (string.IsNullOrWhiteSpace(worldId))
            {
                return null;
            }

            for (int i = 0; i < worlds.Count; i++)
            {
                WorldDefinition world = worlds[i];
                if (world != null && world.Id == worldId)
                {
                    return world;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void SetWorlds(List<WorldDefinition> definitions)
        {
            worlds = definitions ?? new List<WorldDefinition>();
        }
#endif
    }
}
