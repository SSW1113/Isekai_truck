using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterDefinition : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string typeId = "new_monster";
        [SerializeField] private string displayName = "새 몬스터";
        [SerializeField] private Color color = Color.white;

        [Header("Stats")]
        [SerializeField, Min(0.01f)] private float size = 0.6f;
        [SerializeField, Min(0f)] private float speed = 0.04f;
        [SerializeField, Min(0f)] private float fleeDistance = 7f;
        [SerializeField, Min(0)] private int exp = 50;
        [SerializeField, Min(0)] private int soul = 2;
        [SerializeField, Min(0f)] private float spawnWeight = 1f;

        public string TypeId => typeId;
        public string DisplayName => displayName;
        public Color Color => color;
        public float Size => size;
        public float Speed => speed;
        public float FleeDistance => fleeDistance;
        public int Exp => exp;
        public int Soul => soul;
        public float SpawnWeight => spawnWeight;

        public MonsterData CreateData()
        {
            return new MonsterData(
                typeId,
                displayName,
                $"#{ColorUtility.ToHtmlStringRGB(color)}",
                color,
                size,
                speed,
                fleeDistance,
                exp,
                soul,
                spawnWeight
            );
        }

#if UNITY_EDITOR
        public void Configure(MonsterData type)
        {
            typeId = type.Id;
            displayName = type.Name;
            color = type.Color;
            size = type.Size;
            speed = type.Speed;
            fleeDistance = type.FleeDistance;
            exp = type.Exp;
            soul = type.Soul;
            spawnWeight = type.SpawnWeight;
        }
#endif
    }
}
