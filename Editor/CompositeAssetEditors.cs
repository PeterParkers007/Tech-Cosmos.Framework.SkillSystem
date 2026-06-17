#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    [CustomEditor(typeof(CompositeConditionSO))]
    public sealed class CompositeConditionSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开条件树编辑器", GUILayout.Height(24)))
                ConditionTreeEditorWindow.OpenComposite((CompositeConditionSO)target);
            if (GUILayout.Button("打开条件 Graph", GUILayout.Height(24)))
                Graph.TechCosmosGraphEditorWindow.OpenCondition((CompositeConditionSO)target);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            ConditionTreeEditor.DrawRoot(serializedObject, serializedObject.FindProperty("conditionTreeRoot"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(CompositeMechanismSO))]
    public sealed class CompositeMechanismSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开机制树编辑器", GUILayout.Height(24)))
                MechanismTreeEditorWindow.OpenComposite((CompositeMechanismSO)target);
            if (GUILayout.Button("打开机制 Graph", GUILayout.Height(24)))
                Graph.TechCosmosGraphEditorWindow.OpenMechanism((CompositeMechanismSO)target);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            MechanismTreeEditor.DrawRoot(serializedObject, serializedObject.FindProperty("mechanismTreeRoot"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
