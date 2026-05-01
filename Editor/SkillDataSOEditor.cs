#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    [CustomEditor(typeof(SkillDataSO), true)]
    public class SkillDataSOEditor : UnityEditor.Editor
    {
        private SerializedProperty serializedDataProp;
        private Dictionary<string, List<SerializedProperty>> groupedProperties;
        private List<SerializedProperty> ungroupedProperties;

        void OnEnable()
        {
            serializedDataProp = serializedObject.FindProperty("serializedData");
            CacheGroupedProperties();
        }

        private void CacheGroupedProperties()
        {
            groupedProperties = new Dictionary<string, List<SerializedProperty>>();
            ungroupedProperties = new List<SerializedProperty>();

            var property = serializedObject.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    // 跳过已手动绘制的属性
                    if (IsSkippedProperty(property.name)) continue;

                    // 从 Tooltip 中提取分类
                    string category = ExtractCategory(property.tooltip);

                    if (!string.IsNullOrEmpty(category))
                    {
                        if (!groupedProperties.ContainsKey(category))
                            groupedProperties[category] = new List<SerializedProperty>();

                        groupedProperties[category].Add(property.Copy());
                    }
                    else
                    {
                        ungroupedProperties.Add(property.Copy());
                    }

                } while (property.NextVisible(false));
            }
        }

        private bool IsSkippedProperty(string name)
        {
            return name == "m_Script" ||
                   name == "SkillType" ||
                   name == "TriggerEvent" ||
                   name == "SkillName" ||
                   name == "SkillDescription" ||
                   name == "Conditions" ||
                   name == "Mechanisms" ||
                   name == "serializedData";
        }

        private string ExtractCategory(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip)) return null;

            int start = tooltip.IndexOf('[');
            int end = tooltip.IndexOf(']');

            if (start == 0 && end > start)
                return tooltip.Substring(start + 1, end - start - 1);

            return null;
        }

        private string ExtractDisplayName(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip)) return null;

            int end = tooltip.IndexOf(']');
            if (end > 0 && end < tooltip.Length - 1)
                return tooltip.Substring(end + 2).Trim(); // 跳过 "] "

            return tooltip;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制基础属性
            DrawBaseProperties();

            EditorGUILayout.Space(5);

            // 绘制条件和机制
            DrawConditionsAndMechanisms();

            EditorGUILayout.Space(5);

            // 绘制分组属性
            DrawGroupedProperties();

            EditorGUILayout.Space(10);

            // 绘制数值层
            DrawDataLayer();

            serializedObject.ApplyModifiedProperties();

            // 如果有修改，标记为脏
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawBaseProperties()
        {
            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TriggerEvent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillDescription"));

            EditorGUILayout.EndVertical();
        }

        private void DrawConditionsAndMechanisms()
        {
            var conditionsProp = serializedObject.FindProperty("Conditions");
            var mechanismsProp = serializedObject.FindProperty("Mechanisms");

            if (conditionsProp != null)
            {
                EditorGUILayout.LabelField("条件层 (Conditions)", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(conditionsProp, new GUIContent("条件列表"), true);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            if (mechanismsProp != null)
            {
                EditorGUILayout.LabelField("机制层 (Mechanisms)", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(mechanismsProp, new GUIContent("机制列表"), true);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawGroupedProperties()
        {
            if (groupedProperties.Count == 0 && ungroupedProperties.Count == 0)
            {
                EditorGUILayout.HelpBox("没有自定义属性。请使用 [SkillDataField] 标记字段后重新生成。", MessageType.Info);
                return;
            }

            // 绘制分组属性
            foreach (var group in groupedProperties.OrderBy(g => g.Key))
            {
                EditorGUILayout.LabelField($"── {group.Key} ──", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                foreach (var prop in group.Value)
                {
                    string displayName = ExtractDisplayName(prop.tooltip) ?? prop.displayName;
                    EditorGUILayout.PropertyField(prop, new GUIContent(displayName));
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            // 绘制未分组属性
            if (ungroupedProperties.Count > 0)
            {
                EditorGUILayout.LabelField("── 其他属性 ──", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                foreach (var prop in ungroupedProperties)
                {
                    EditorGUILayout.PropertyField(prop, true);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawDataLayer()
        {
            EditorGUILayout.LabelField("数值层 (Data)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (serializedDataProp != null)
            {
                for (int i = 0; i < serializedDataProp.arraySize; i++)
                {
                    var element = serializedDataProp.GetArrayElementAtIndex(i);
                    DrawDataEntry(element, i);
                }
            }

            EditorGUILayout.EndVertical();

            // 添加按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Float", GUILayout.Height(20)))
                AddDataEntry("newFloat", new FloatValue());
            if (GUILayout.Button("+ Int", GUILayout.Height(20)))
                AddDataEntry("newInt", new IntValue());
            if (GUILayout.Button("+ String", GUILayout.Height(20)))
                AddDataEntry("newString", new StringValue());
            if (GUILayout.Button("+ Bool", GUILayout.Height(20)))
                AddDataEntry("newBool", new BoolValue());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDataEntry(SerializedProperty element, int index)
        {
            var keyProp = element.FindPropertyRelative("key");
            var containerProp = element.FindPropertyRelative("valueContainer");

            EditorGUILayout.BeginHorizontal();

            // Key 输入
            EditorGUILayout.LabelField("Key", GUILayout.Width(25));
            keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.Width(100));

            // Value
            if (containerProp.managedReferenceValue != null)
            {
                var valueProp = containerProp.FindPropertyRelative("value");
                if (valueProp != null)
                {
                    string typeLabel = containerProp.managedReferenceValue switch
                    {
                        FloatValue => "Float",
                        IntValue => "Int",
                        StringValue => "Str",
                        BoolValue => "Bool",
                        _ => "?"
                    };

                    EditorGUILayout.LabelField(typeLabel, GUILayout.Width(35));

                    switch (containerProp.managedReferenceValue)
                    {
                        case FloatValue:
                            valueProp.floatValue = EditorGUILayout.FloatField(valueProp.floatValue);
                            break;
                        case IntValue:
                            valueProp.intValue = EditorGUILayout.IntField(valueProp.intValue);
                            break;
                        case StringValue:
                            valueProp.stringValue = EditorGUILayout.TextField(valueProp.stringValue);
                            break;
                        case BoolValue:
                            valueProp.boolValue = EditorGUILayout.Toggle(valueProp.boolValue);
                            break;
                    }
                }
            }
            else
            {
                // 没有值时，占位
                EditorGUILayout.LabelField("(未设置)", GUILayout.Width(60));
            }

            // 删除按钮
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                serializedDataProp.DeleteArrayElementAtIndex(index);
            }
            GUI.backgroundColor = oldColor;

            EditorGUILayout.EndHorizontal();
        }

        private void AddDataEntry(string key, ValueContainer container)
        {
            int index = serializedDataProp.arraySize;
            serializedDataProp.InsertArrayElementAtIndex(index);

            var element = serializedDataProp.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("key").stringValue = key;
            element.FindPropertyRelative("valueContainer").managedReferenceValue = container;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif