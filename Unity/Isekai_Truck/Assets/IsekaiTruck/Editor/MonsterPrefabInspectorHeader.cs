using System;
using IsekaiTruck.Monsters;
using UnityEditor;
using UnityEngine;

namespace IsekaiTruck.Editor
{
    [InitializeOnLoad]
    public static class MonsterPrefabInspectorHeader
    {
        private const string PrefabFolderPath = "Assets/IsekaiTruck/Prefabs/Monsters/";

        private static UnityEditor.Editor definitionEditor;

        static MonsterPrefabInspectorHeader()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= DrawMonsterStats;
            UnityEditor.Editor.finishedDefaultHeaderGUI += DrawMonsterStats;
        }

        private static void DrawMonsterStats(UnityEditor.Editor editor)
        {
            if (editor.target is not GameObject prefabObject)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(prefabObject);
            if (!assetPath.StartsWith(PrefabFolderPath, StringComparison.Ordinal))
            {
                return;
            }

            MonsterDefinition definition = prefabObject.GetComponent<MonsterDefinition>();
            if (definition == null)
            {
                return;
            }

            UnityEditor.Editor.CreateCachedEditor(definition, typeof(MonsterDefinitionEditor), ref definitionEditor);

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            definitionEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }
    }
}
