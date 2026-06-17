#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>机制树独立编辑窗口。</summary>
    public sealed class MechanismTreeEditorWindow : EditorWindow
    {
        private ScriptableObject _target;
        private SerializedObject _serializedObject;
        private SerializedProperty _rootProp;
        private Vector2 _scroll;

        public static void OpenSkill(SkillDataSO skill)
        {
            if (skill == null) return;
            var window = GetWindow<MechanismTreeEditorWindow>(true, "机制树编辑器", true);
            window.minSize = new Vector2(520, 420);
            window.Bind(skill, "mechanismTreeRoot", skill.name);
            window.ShowUtility();
        }

        public static void OpenComposite(CompositeMechanismSO preset)
        {
            if (preset == null) return;
            var window = GetWindow<MechanismTreeEditorWindow>(true, "复合机制编辑器", true);
            window.minSize = new Vector2(520, 420);
            window.Bind(preset, "mechanismTreeRoot", preset.displayName);
            window.ShowUtility();
        }

        private void Bind(ScriptableObject target, string rootPropertyName, string title)
        {
            _target = target;
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(target);
            _rootProp = _serializedObject.FindProperty(rootPropertyName);
            titleContent = new GUIContent($"机制树 · {title}");
        }

        private void OnGUI()
        {
            if (_serializedObject == null || _rootProp == null)
            {
                EditorGUILayout.HelpBox("未绑定机制树目标。", MessageType.Info);
                return;
            }

            _serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            if (_target is CompositeMechanismSO preset && GUILayout.Button("打开机制 Graph", GUILayout.Height(24)))
            {
                Graph.TechCosmosGraphEditorWindow.OpenMechanism(preset);
            }
            if (_target is SkillDataSO skill && GUILayout.Button("打开技能 Graph", GUILayout.Height(24)))
            {
                Graph.TechCosmosGraphEditorWindow.OpenSkill(skill);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_target is SkillDataSO)
                MechanismTreeEditor.Draw(_serializedObject, _target as SkillDataSO);
            else
                MechanismTreeEditor.DrawRoot(_serializedObject, _rootProp);
            EditorGUILayout.EndScrollView();

            _serializedObject.ApplyModifiedProperties();
        }

        private void OnDisable()
        {
            _serializedObject?.ApplyModifiedProperties();
            if (_target != null)
                EditorUtility.SetDirty(_target);
        }
    }
}
#endif
