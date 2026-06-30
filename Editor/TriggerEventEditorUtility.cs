#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// TriggerEvents 字段的统一 Inspector 绘制（MaskField + 安全回退）。
    /// </summary>
    public static class TriggerEventEditorUtility
    {
        private const int MaskFieldLimit = 31;

        public static Type GetTriggerEventEnumType()
        {
            return SkillSystemEnumGenerator.ResolveRuntimeEnumType(SkillSystemEnumKind.TriggerEvent);
        }

        public static void DrawTriggerEventField(SerializedProperty triggerEventsProp, SerializedObject serializedObject)
        {
            if (triggerEventsProp == null)
                return;

            var enumType = GetTriggerEventEnumType();
            if (enumType == null)
            {
                EditorGUILayout.PropertyField(triggerEventsProp, new GUIContent("触发事件列表"), true);
                return;
            }

            var optionNames = Enum.GetNames(enumType).Where(n => n != "None").ToList();
            var currentEvents = new List<string>();

            for (int i = 0; i < triggerEventsProp.arraySize; i++)
            {
                var evt = triggerEventsProp.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrEmpty(evt)) continue;
                currentEvents.Add(evt);
                if (!optionNames.Contains(evt))
                    optionNames.Add(evt);
            }

            if (optionNames.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "TriggerEventType 枚举为空。请运行 Tech-Cosmos → SkillSystem → Enum Editor 添加事件。",
                    MessageType.Warning);
                EditorGUILayout.PropertyField(triggerEventsProp, new GUIContent("触发事件"), true);
                return;
            }

            if (optionNames.Count > MaskFieldLimit)
            {
                EditorGUILayout.HelpBox(
                    $"触发事件超过 {MaskFieldLimit} 个，MaskField 无法完整显示，已切换为列表编辑。",
                    MessageType.Info);
                EditorGUILayout.PropertyField(triggerEventsProp, new GUIContent("触发事件"), true);
                return;
            }

            var displayedOptions = optionNames.ToArray();
            int mask = 0;
            for (int i = 0; i < displayedOptions.Length; i++)
            {
                if (currentEvents.Contains(displayedOptions[i]))
                    mask |= (1 << i);
            }

            int newMask = EditorGUILayout.MaskField("触发事件", mask, displayedOptions);
            if (newMask == mask)
                return;

            triggerEventsProp.ClearArray();
            int index = 0;
            for (int i = 0; i < displayedOptions.Length; i++)
            {
                if ((newMask & (1 << i)) == 0) continue;
                triggerEventsProp.InsertArrayElementAtIndex(index);
                triggerEventsProp.GetArrayElementAtIndex(index).stringValue = displayedOptions[i];
                index++;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
