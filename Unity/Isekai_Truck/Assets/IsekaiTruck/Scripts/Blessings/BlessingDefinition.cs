using UnityEngine;

namespace IsekaiTruck.Blessings
{
    [CreateAssetMenu(fileName = "Blessing", menuName = "Isekai Truck/Blessing Definition")]
    public sealed class BlessingDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private BlessingGrade grade;
        [SerializeField] private BlessingActivationType activationType;
        [SerializeField] private BlessingEffectType effectType;
        [SerializeField] private float effectValue = 1f;
        [SerializeField, Min(0f)] private float duration;
        [SerializeField, Min(0f)] private float interval;
        [SerializeField, Min(0f)] private float radius;
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => displayName;
        public BlessingGrade Grade => grade;
        public BlessingActivationType ActivationType => activationType;
        public BlessingEffectType EffectType => effectType;
        public float EffectValue => effectValue;
        public float Duration => duration;
        public float Interval => interval;
        public float Radius => radius;
        public string Description => description;

#if UNITY_EDITOR
        public void Configure(string blessingId, string blessingName, BlessingGrade blessingGrade, string blessingDescription)
        {
            Configure(blessingId, blessingName, blessingGrade, BlessingActivationType.Passive, BlessingEffectType.None, 1f, 0f, 0f, 0f, blessingDescription);
        }

        public void Configure(
            string blessingId,
            string blessingName,
            BlessingGrade blessingGrade,
            BlessingActivationType blessingActivationType,
            BlessingEffectType blessingEffectType,
            float blessingEffectValue,
            float blessingDuration,
            float blessingInterval,
            float blessingRadius,
            string blessingDescription
        )
        {
            id = blessingId;
            displayName = blessingName;
            grade = blessingGrade;
            activationType = blessingActivationType;
            effectType = blessingEffectType;
            effectValue = blessingEffectValue;
            duration = blessingDuration;
            interval = blessingInterval;
            radius = blessingRadius;
            description = blessingDescription;
        }
#endif
    }

    public enum BlessingGrade
    {
        C,
        U,
        R,
        SR
    }

    public enum BlessingActivationType
    {
        Passive,
        Active
    }

    public enum BlessingEffectType
    {
        None,
        MonsterSlow,
        VisionBoost,
        PeriodicStun,
        ExperienceGain,
        TruckBoost,
        TruckSpeed,
        TruckSize,
        TimeStop
    }
}
