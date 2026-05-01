#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;
namespace TechCosmos.SkillSystem.Editor
{
    [CustomEditor(typeof(SkillDataSO), true)]
    public class SkillDataSOEditor : UnityEditor.Editor
    {
        private SerializedProperty serializedDataProp;

        void OnEnable()
        {
            serializedDataProp = serializedObject.FindProperty("serializedData");
        }

        public override void OnInspectorGUI()
        {
            // 绘制基础属性（SkillType, TriggerEvent 等）
            DrawPropertiesExcluding(serializedObject, "serializedData");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("数值层 (Data)", EditorStyles.boldLabel);

            serializedObject.Update();

            if (serializedDataProp != null)
            {
                DrawDataList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDataList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < serializedDataProp.arraySize; i++)
            {
                var element = serializedDataProp.GetArrayElementAtIndex(i);
                var keyProp = element.FindPropertyRelative("key");
                var containerProp = element.FindPropertyRelative("valueContainer");

                EditorGUILayout.BeginHorizontal();

                // Key
                keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.Width(120));

                // Value 类型标签和输入
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

                // 删除
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    serializedDataProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // 添加按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Float")) AddEntry("newFloat", new FloatValue());
            if (GUILayout.Button("+ Int")) AddEntry("newInt", new IntValue());
            if (GUILayout.Button("+ String")) AddEntry("newString", new StringValue());
            if (GUILayout.Button("+ Bool")) AddEntry("newBool", new BoolValue());
            EditorGUILayout.EndHorizontal();
        }

        private void AddEntry(string key, ValueContainer container)
        {
            int index = serializedDataProp.arraySize;
            serializedDataProp.InsertArrayElementAtIndex(index);

            var element = serializedDataProp.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("key").stringValue = key;
            element.FindPropertyRelative("valueContainer").managedReferenceValue = container;
        }
    }
}
#endif