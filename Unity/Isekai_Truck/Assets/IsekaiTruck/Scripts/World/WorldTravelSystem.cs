using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.Wanted;
using UnityEngine;

namespace IsekaiTruck.World
{
    [DisallowMultipleComponent]
    public sealed class WorldTravelSystem : MonoBehaviour
    {
        [SerializeField] private WorldCatalog worldCatalog;

        private GameConfig.WantedSettings wantedSettings;
        private WantedLevelSystem wantedLevelSystem;
        private WorldDefinition currentWorld;
        private bool isTraveling;

        public WorldDefinition CurrentWorld => currentWorld;
        public int RequiredWantedLevel => wantedSettings.WorldTravelUnlockLevel;
        public bool HasOtherWorld => worldCatalog != null && worldCatalog.Worlds.Count > 1;
        public bool CanTravel => wantedLevelSystem != null && wantedLevelSystem.Level >= RequiredWantedLevel && HasOtherWorld;

        public event Action<WorldTravelSnapshot> StateChanged;
        public event Action<WorldDefinition> WorldChanged;

        public void Initialize(GameConfig gameConfig, WantedLevelSystem wanted)
        {
            if (worldCatalog == null || worldCatalog.Worlds.Count == 0)
            {
                throw new MissingReferenceException("WorldTravelSystem에 세계 카탈로그가 연결되지 않았습니다.");
            }

            ValidateCatalog();
            wantedSettings = gameConfig.Wanted;
            wantedLevelSystem = wanted;
            currentWorld = worldCatalog.Worlds[0];
            wantedLevelSystem.StateChanged += HandleWantedStateChanged;
        }

        public bool TryTravel(out WorldTravelResult result)
        {
            result = default;
            if (!CanTravel)
            {
                return false;
            }

            WorldDefinition previousWorld = currentWorld;
            WorldDefinition destination = ChooseRandomOtherWorld();
            if (destination == null)
            {
                return false;
            }

            currentWorld = destination;
            isTraveling = true;
            wantedLevelSystem.ResetForWorldTravel();
            isTraveling = false;

            result = new WorldTravelResult(previousWorld, destination);
            WorldChanged?.Invoke(destination);
            StateChanged?.Invoke(GetState());
            return true;
        }

        public void RestoreState(string savedWorldId)
        {
            WorldDefinition restoredWorld = worldCatalog.FindById(savedWorldId);
            currentWorld = restoredWorld != null ? restoredWorld : worldCatalog.Worlds[0];
            StateChanged?.Invoke(GetState());
        }

        public WorldTravelSnapshot GetState()
        {
            return new WorldTravelSnapshot(currentWorld, RequiredWantedLevel, CanTravel, HasOtherWorld);
        }

        private WorldDefinition ChooseRandomOtherWorld()
        {
            IReadOnlyList<WorldDefinition> worlds = worldCatalog.Worlds;
            int selection = UnityEngine.Random.Range(0, worlds.Count - 1);

            for (int i = 0; i < worlds.Count; i++)
            {
                if (worlds[i] == currentWorld)
                {
                    continue;
                }

                if (selection == 0)
                {
                    return worlds[i];
                }

                selection--;
            }

            return null;
        }

        private void ValidateCatalog()
        {
            HashSet<string> ids = new HashSet<string>();
            IReadOnlyList<WorldDefinition> worlds = worldCatalog.Worlds;
            for (int i = 0; i < worlds.Count; i++)
            {
                WorldDefinition world = worlds[i];
                if (world == null)
                {
                    throw new MissingReferenceException($"세계 카탈로그의 {i}번 항목이 비어 있습니다.");
                }

                if (string.IsNullOrWhiteSpace(world.Id))
                {
                    throw new InvalidOperationException($"세계 ID가 비어 있습니다: {world.name}");
                }

                if (!ids.Add(world.Id))
                {
                    throw new InvalidOperationException($"세계 ID가 중복되었습니다: {world.Id}");
                }
            }
        }

        private void HandleWantedStateChanged(WantedLevelSnapshot state)
        {
            if (!isTraveling)
            {
                StateChanged?.Invoke(GetState());
            }
        }

        private void OnDestroy()
        {
            if (wantedLevelSystem != null)
            {
                wantedLevelSystem.StateChanged -= HandleWantedStateChanged;
            }
        }

#if UNITY_EDITOR
        public void SetCatalog(WorldCatalog catalog)
        {
            worldCatalog = catalog;
        }
#endif
    }

    public readonly struct WorldTravelSnapshot
    {
        public WorldTravelSnapshot(WorldDefinition currentWorld, int requiredWantedLevel, bool canTravel, bool hasOtherWorld)
        {
            CurrentWorld = currentWorld;
            RequiredWantedLevel = requiredWantedLevel;
            CanTravel = canTravel;
            HasOtherWorld = hasOtherWorld;
        }

        public WorldDefinition CurrentWorld { get; }
        public int RequiredWantedLevel { get; }
        public bool CanTravel { get; }
        public bool HasOtherWorld { get; }
    }

    public readonly struct WorldTravelResult
    {
        public WorldTravelResult(WorldDefinition previousWorld, WorldDefinition destinationWorld)
        {
            PreviousWorld = previousWorld;
            DestinationWorld = destinationWorld;
        }

        public WorldDefinition PreviousWorld { get; }
        public WorldDefinition DestinationWorld { get; }
    }
}
