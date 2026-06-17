#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using TechCosmos.SkillSystem.Runtime;
#if UNITY_EDITOR

namespace TechCosmos.SkillSystem.Editor
{
    [CustomPropertyDrawer(typeof(ExecutionModeBase), true)]
    public class ExecutionModeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.managedReferenceValue == null)
            {
                var labelRect = new Rect(position.x, position.y, position.width - 100, EditorGUIUtility.singleLineHeight);
                var buttonRect = new Rect(position.x + position.width - 95, position.y, 95, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, label);

                if (GUI.Button(buttonRect, "Select Mode"))
                {
                    ShowAddMenu(property);
                }
            }
            else
            {
                var typeName = ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name);

                // �۵� + ����
                var foldoutRect = new Rect(position.x, position.y, position.width - 160, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"{typeName}", true);

                // Switch / Copy / Remove
                float btnWidth = 52;
                float btnX = position.x + position.width - 155;

                if (GUI.Button(new Rect(btnX, position.y, btnWidth, EditorGUIUtility.singleLineHeight), "Switch"))
                    ShowAddMenu(property);

                if (GUI.Button(new Rect(btnX + btnWidth + 2, position.y, btnWidth, EditorGUIUtility.singleLineHeight), "Copy"))
                {
                    string json = JsonUtility.ToJson(property.managedReferenceValue);
                    EditorGUIUtility.systemCopyBuffer = json;
                    Debug.Log($"�Ѹ���: {typeName}");
                }

                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUI.Button(new Rect(btnX + (btnWidth + 2) * 2, position.y, btnWidth, EditorGUIUtility.singleLineHeight), "Remove"))
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
                GUI.backgroundColor = oldColor;

                // չ����������л��ֶ�
                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    var propRect = new Rect(
                        position.x + 15,
                        position.y + EditorGUIUtility.singleLineHeight + 2,
                        position.width - 15,
                        position.height - EditorGUIUtility.singleLineHeight - 2);
                    EditorGUI.PropertyField(propRect, property, GUIContent.none, true);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight + 4;
        }

        private void ShowAddMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), false, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });
            menu.AddSeparator("");

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetExportedTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSerializable && !t.IsGenericTypeDefinition)
                .Where(t => typeof(ExecutionModeBase).IsAssignableFrom(t))
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in types)
            {
                var capturedType = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(capturedType);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}
#endif
namespace TechCosmos.SkillSystem.Editor
{
    [CustomPropertyDrawer(typeof(BuffEffectExecuterBase), true)]
    public class BuffEffectExecuterDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var modeProp = property.FindPropertyRelative("executionMode");
            var effectsProp = property.FindPropertyRelative("effects");

            string modeName = (modeProp != null && modeProp.managedReferenceValue != null)
                ? ObjectNames.NicifyVariableName(modeProp.managedReferenceValue.GetType().Name)
                : "δѡ��";
            int effectCount = effectsProp?.arraySize ?? 0;

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded,
                $"{label.text} �� ģʽ: {modeName}��{effectCount} ��Ч����", true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + EditorGUIUtility.singleLineHeight + 4;

                if (modeProp != null)
                {
                    float h = EditorGUI.GetPropertyHeight(modeProp, true);
                    var r = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(r, modeProp, new GUIContent("ִ��ģʽ"), true);
                    y += h + 2;
                }

                if (effectsProp != null)
                {
                    float h = EditorGUI.GetPropertyHeight(effectsProp, true);
                    var r = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(r, effectsProp, new GUIContent("Ч���б�"), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + 2;

            if (property.isExpanded)
            {
                var modeProp = property.FindPropertyRelative("executionMode");
                var effectsProp = property.FindPropertyRelative("effects");

                if (modeProp != null)
                    height += EditorGUI.GetPropertyHeight(modeProp, true) + 2;
                if (effectsProp != null)
                    height += EditorGUI.GetPropertyHeight(effectsProp, true) + 2;
            }

            return height;
        }
    }

    [CustomPropertyDrawer(typeof(BuffEffectBase), true)]
    public class BuffEffectDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.managedReferenceValue == null)
            {
                var labelRect = new Rect(position.x, position.y, position.width - 100, EditorGUIUtility.singleLineHeight);
                var buttonRect = new Rect(position.x + position.width - 95, position.y, 95, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, label);

                if (GUI.Button(buttonRect, "Select Effect"))
                {
                    ShowAddMenu(property);
                }
            }
            else
            {
                var typeName = ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name);
                property.isExpanded = EditorGUI.Foldout(
                    new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                    property.isExpanded, $"{label.text}: {typeName}", true);

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    var buttonY = position.y + EditorGUIUtility.singleLineHeight + 2;
                    var buttonRect = new Rect(position.x + 15, buttonY, position.width - 15, EditorGUIUtility.singleLineHeight);
                    float halfWidth = buttonRect.width / 2;

                    if (GUI.Button(new Rect(buttonRect.x, buttonRect.y, halfWidth - 2, buttonRect.height), "Switch"))
                        ShowAddMenu(property);

                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                    if (GUI.Button(new Rect(buttonRect.x + halfWidth, buttonRect.y, halfWidth - 2, buttonRect.height), "Remove"))
                    {
                        property.managedReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    GUI.backgroundColor = oldColor;

                    var propRect = new Rect(
                        position.x + 15,
                        buttonY + EditorGUIUtility.singleLineHeight + 4,
                        position.width - 15,
                        position.height - EditorGUIUtility.singleLineHeight * 2 - 6);
                    EditorGUI.PropertyField(propRect, property, GUIContent.none, true);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight * 2 + 6;
        }

        private void ShowAddMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), false, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });
            menu.AddSeparator("");

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetExportedTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSerializable && !t.IsGenericTypeDefinition)
                .Where(t => typeof(BuffEffectBase).IsAssignableFrom(t))
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .ToList();

            if (types.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("û�п��õ� BuffEffect"));
            }
            else
            {
                var grouped = types
                    .GroupBy(t =>
                    {
                        var attr = t.GetCustomAttributes(typeof(BuffEffectMenuAttribute), false)
                            .FirstOrDefault() as BuffEffectMenuAttribute;
                        return attr?.Category ?? "Other";
                    })
                    .OrderBy(g => g.Key);

                foreach (var group in grouped)
                {
                    foreach (var type in group)
                    {
                        var capturedType = type;
                        var attr = type.GetCustomAttributes(typeof(BuffEffectMenuAttribute), false)
                            .FirstOrDefault() as BuffEffectMenuAttribute;
                        var displayName = attr?.DisplayName ?? ObjectNames.NicifyVariableName(type.Name);
                        var path = $"{group.Key}/{displayName}";

                        menu.AddItem(new GUIContent(path), false, () =>
                        {
                            property.managedReferenceValue = Activator.CreateInstance(capturedType);
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
            }

            menu.ShowAsContext();
        }
    }
}
#endif