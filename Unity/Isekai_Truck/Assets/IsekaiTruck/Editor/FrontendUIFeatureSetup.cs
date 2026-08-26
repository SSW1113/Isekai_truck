using UnityEditor;
using UnityEngine;

namespace IsekaiTruck.Editor
{
    public static class FrontendUIFeatureSetup
    {
        [MenuItem("Isekai Truck/Apply Frontend UI Features")]
        public static void Setup()
        {
            SeventhStageSetup.Setup();
            RebirthFeatureSetup.Setup();
            BlessingSkillFeatureSetup.Setup();
            EnemyFeatureSetup.Setup();
            WorldTravelFeatureSetup.Setup();
            WantedLevelFeatureSetup.Setup();
            CollisionFeedbackFeatureSetup.Setup();
            MonsterCollectionFeatureSetup.Setup();
            MainHudLayoutSetup.Setup();
            SystemGuideFeatureSetup.Setup();
            StoryIntroFeatureSetup.Setup();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "프론트엔드 UI 스타일과 피드백 기능을 적용했습니다.", "확인");
            }
        }
    }
}
