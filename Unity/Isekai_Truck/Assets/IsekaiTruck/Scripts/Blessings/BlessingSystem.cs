using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Blessings
{
    [DisallowMultipleComponent]
    public sealed class BlessingSystem : MonoBehaviour
    {
        [SerializeField] private BlessingCatalog catalog;

        private readonly Dictionary<string, int> ownedCounts = new Dictionary<string, int>();
        private readonly List<BlessingDefinition> pendingCandidates = new List<BlessingDefinition>();

        public IReadOnlyList<BlessingDefinition> PendingCandidates => pendingCandidates;
        public IReadOnlyList<BlessingDefinition> Definitions => catalog.Definitions;
        public bool HasPendingCandidates => pendingCandidates.Count > 0;
        public int TotalOwnedCount { get; private set; }

        public event Action StateChanged;

        public void Initialize()
        {
            ownedCounts.Clear();
            pendingCandidates.Clear();
            TotalOwnedCount = 0;
        }

        public bool CreateCandidates(GameConfig.RebirthTierSettings tier, int candidateCount)
        {
            pendingCandidates.Clear();

            if (catalog == null || catalog.Definitions.Count < candidateCount)
            {
                Debug.LogError("Blessing catalog does not contain enough definitions.", this);
                return false;
            }

            for (int i = 0; i < candidateCount; i++)
            {
                BlessingGrade grade = RollGrade(tier);
                BlessingDefinition definition = PickUniqueDefinition(grade);
                if (definition == null)
                {
                    pendingCandidates.Clear();
                    return false;
                }

                pendingCandidates.Add(definition);
            }

            StateChanged?.Invoke();
            return true;
        }

        public BlessingDefinition ChooseCandidate(int candidateIndex)
        {
            if (candidateIndex < 0 || candidateIndex >= pendingCandidates.Count)
            {
                return null;
            }

            BlessingDefinition selected = pendingCandidates[candidateIndex];
            ownedCounts.TryGetValue(selected.Id, out int count);
            ownedCounts[selected.Id] = count + 1;
            TotalOwnedCount++;
            pendingCandidates.Clear();
            StateChanged?.Invoke();
            return selected;
        }

        public int GetOwnedCount(string blessingId)
        {
            return ownedCounts.TryGetValue(blessingId, out int count) ? count : 0;
        }

        public BlessingDefinition FindDefinition(string blessingId)
        {
            return catalog.FindById(blessingId);
        }

        public bool TryRemoveOwned(string blessingId)
        {
            if (!ownedCounts.TryGetValue(blessingId, out int count) || count <= 0)
            {
                return false;
            }

            if (count == 1)
            {
                ownedCounts.Remove(blessingId);
            }
            else
            {
                ownedCounts[blessingId] = count - 1;
            }

            TotalOwnedCount--;
            StateChanged?.Invoke();
            return true;
        }

        public List<OwnedBlessingData> GetOwnedSnapshot()
        {
            List<OwnedBlessingData> snapshot = new List<OwnedBlessingData>(ownedCounts.Count);
            foreach (KeyValuePair<string, int> pair in ownedCounts)
            {
                snapshot.Add(new OwnedBlessingData(pair.Key, pair.Value));
            }

            return snapshot;
        }

        public List<string> GetPendingCandidateIds()
        {
            List<string> ids = new List<string>(pendingCandidates.Count);
            for (int i = 0; i < pendingCandidates.Count; i++)
            {
                ids.Add(pendingCandidates[i].Id);
            }

            return ids;
        }

        public void RestoreState(IReadOnlyList<OwnedBlessingData> owned, IReadOnlyList<string> pendingIds)
        {
            ownedCounts.Clear();
            pendingCandidates.Clear();
            TotalOwnedCount = 0;

            if (owned != null)
            {
                for (int i = 0; i < owned.Count; i++)
                {
                    OwnedBlessingData entry = owned[i];
                    if (entry == null || string.IsNullOrEmpty(entry.id) || entry.count <= 0 || catalog.FindById(entry.id) == null)
                    {
                        continue;
                    }

                    ownedCounts[entry.id] = entry.count;
                    TotalOwnedCount += entry.count;
                }
            }

            if (pendingIds != null)
            {
                for (int i = 0; i < pendingIds.Count; i++)
                {
                    BlessingDefinition definition = catalog.FindById(pendingIds[i]);
                    if (definition != null && !pendingCandidates.Contains(definition))
                    {
                        pendingCandidates.Add(definition);
                    }
                }
            }

            StateChanged?.Invoke();
        }

        private BlessingGrade RollGrade(GameConfig.RebirthTierSettings tier)
        {
            float totalWeight = tier.TotalWeight;
            float roll = UnityEngine.Random.Range(0f, totalWeight);

            if (roll < tier.CWeight) return BlessingGrade.C;
            roll -= tier.CWeight;
            if (roll < tier.UWeight) return BlessingGrade.U;
            roll -= tier.UWeight;
            if (roll < tier.RWeight) return BlessingGrade.R;
            return BlessingGrade.SR;
        }

        private BlessingDefinition PickUniqueDefinition(BlessingGrade grade)
        {
            List<BlessingDefinition> matches = new List<BlessingDefinition>();
            CollectAvailableDefinitions(matches, grade, true);
            if (matches.Count == 0)
            {
                CollectAvailableDefinitions(matches, grade, false);
            }

            return matches.Count > 0 ? matches[UnityEngine.Random.Range(0, matches.Count)] : null;
        }

        private void CollectAvailableDefinitions(List<BlessingDefinition> matches, BlessingGrade grade, bool matchGrade)
        {
            IReadOnlyList<BlessingDefinition> definitions = catalog.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                BlessingDefinition definition = definitions[i];
                if (definition == null || pendingCandidates.Contains(definition))
                {
                    continue;
                }

                if (!matchGrade || definition.Grade == grade)
                {
                    matches.Add(definition);
                }
            }
        }

#if UNITY_EDITOR
        public void SetCatalog(BlessingCatalog blessingCatalog)
        {
            catalog = blessingCatalog;
        }

        public void AddOwnedForVerification(string blessingId, int count)
        {
            if (count <= 0 || catalog.FindById(blessingId) == null)
            {
                return;
            }

            ownedCounts.TryGetValue(blessingId, out int currentCount);
            ownedCounts[blessingId] = currentCount + count;
            TotalOwnedCount += count;
            StateChanged?.Invoke();
        }
#endif
    }

    [Serializable]
    public sealed class OwnedBlessingData
    {
        public string id;
        public int count;

        public OwnedBlessingData(string blessingId, int ownedCount)
        {
            id = blessingId;
            count = ownedCount;
        }
    }
}
