#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>条件树独立编辑窗口，支持技能 SO 与复合条件资产。</summary>
    public sealed class ConditionTreeEditorWindow : EditorWindow
    {
        private ScriptableObject _target;
        private SerializedObject _serializedObject;
        private SerializedProperty _rootProp;
        private Vector2 _scroll;

        public static void OpenSkill(SkillDataSO skill)
        {
            if (skill == null) return;
            var window = GetWindow<ConditionTreeEditorWindow>(true, "条件树编辑器", true);
            window.minSize = new Vector2(520, 420);
            window.Bind(skill, "conditionTreeRoot", skill.name);
            window.ShowUtility();
        }

        public static void OpenComposite(CompositeConditionSO preset)
        {
            if (preset == null) return;
            var window = GetWindow<ConditionTreeEditorWindow>(true, "复合条件编辑器", true);
            window.minSize = new Vector2(520, 420);
            window.Bind(preset, "conditionTreeRoot", preset.displayName);
            window.ShowUtility();
        }

        private void Bind(ScriptableObject target, string rootPropertyName, string title)
        {
            _target = target;
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(target);
            _rootProp = _serializedObject.FindProperty(rootPropertyName);
            titleContent = new GUIContent($"条件树 · {title}");
        }

        private void OnGUI()
        {
            if (_serializedObject == null || _rootProp == null)
            {
                EditorGUILayout.HelpBox("未绑定条件树目标。", MessageType.Info);
                return;
            }

            _serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            if (_target is CompositeConditionSO preset && GUILayout.Button("打开条件 Graph", GUILayout.Height(24)))
            {
                Graph.TechCosmosGraphEditorWindow.OpenCondition(preset);
            }
            if (_target is SkillDataSO skill && GUILayout.Button("打开技能 Graph", GUILayout.Height(24)))
            {
                Graph.TechCosmosGraphEditorWindow.OpenSkill(skill);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_target is SkillDataSO)
                ConditionTreeEditor.Draw(_serializedObject, _target as SkillDataSO);
            else
                ConditionTreeEditor.DrawRoot(_serializedObject, _rootProp, showUseTreeToggle: false);
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
