using System.Collections.Generic;
using UnityEngine;

namespace IsekaiTruck.Blessings
{
    [CreateAssetMenu(fileName = "BlessingCatalog", menuName = "Isekai Truck/Blessing Catalog")]
    public sealed class BlessingCatalog : ScriptableObject
    {
        [SerializeField] private List<BlessingDefinition> definitions = new List<BlessingDefinition>();

        public IReadOnlyList<BlessingDefinition> Definitions => definitions;

        public BlessingDefinition FindById(string id)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                BlessingDefinition definition = definitions[i];
                if (definition != null && definition.Id == id)
                {
                    return definition;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void SetDefinitions(List<BlessingDefinition> blessingDefinitions)
        {
            definitions = blessingDefinitions;
        }
#endif
    }
}
