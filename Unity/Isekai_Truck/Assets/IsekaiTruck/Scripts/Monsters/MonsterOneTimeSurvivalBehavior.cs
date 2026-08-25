using IsekaiTruck.Audio;
using IsekaiTruck.Visuals;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterOneTimeSurvivalBehavior : MonsterContactBehavior
    {
        [Header("One-Time Survival")]
        [SerializeField, Min(0f)] private float teleportDistanceMultiplier = 2f;
        [SerializeField] private SpriteSequenceEffect substitutionEffectPrefab;

        private bool hasSurvived;

        public float TeleportDistanceMultiplier => teleportDistanceMultiplier;
        public SpriteSequenceEffect SubstitutionEffectPrefab => substitutionEffectPrefab;
        public bool HasSurvived => hasSurvived;

        protected override void OnInitialized()
        {
            hasSurvived = false;
        }

        protected override bool TryResolveContact(
            MonsterContactContext context,
            out MonsterContactResult result)
        {
            if (hasSurvived)
            {
                result = MonsterContactResult.Defeated;
                return true;
            }

            hasSurvived = true;
            GameSfxPlayer.PlayNinjaSubstitution();
            if (substitutionEffectPrefab != null)
            {
                Instantiate(substitutionEffectPrefab, transform.position, Quaternion.identity);
            }

            Vector3 escapeDirection = transform.position - context.Truck.position;
            escapeDirection.y = 0f;
            if (escapeDirection.sqrMagnitude <= 0.000001f)
            {
                escapeDirection = -Vector3.ProjectOnPlane(context.TruckForward, Vector3.up);
                if (escapeDirection.sqrMagnitude <= 0.000001f)
                {
                    escapeDirection = Vector3.forward;
                }
            }

            float teleportDistance = Type.FleeDistance * teleportDistanceMultiplier;
            transform.position += escapeDirection.normalized * teleportDistance;
            result = new MonsterContactResult(MonsterContactOutcome.Survived);
            return true;
        }
    }
}
