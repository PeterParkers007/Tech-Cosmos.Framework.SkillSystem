// Editor/FormulaDrawer.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    [CustomPropertyDrawer(typeof(FormulaValue))]
    public class FormulaValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var formulaTypeProp = property.FindPropertyRelative("formulaType");
            var staticValueProp = property.FindPropertyRelative("staticValue");
            var referencePathProp = property.FindPropertyRelative("referencePath");
            var multiplierProp = property.FindPropertyRelative("multiplier");
            var offsetProp = property.FindPropertyRelative("offset");
            var operatorTypeProp = property.FindPropertyRelative("operatorType");
            var customFormulaProp = property.FindPropertyRelative("customFormula");

            var type = (FormulaValue.FormulaType)formulaTypeProp.enumValueIndex;

            // 类型选择 + 值输入
            var typeRect = new Rect(position.x, position.y, 80, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(position.x + 85, position.y, position.width - 85, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(typeRect, formulaTypeProp, GUIContent.none);

            switch (type)
            {
                case FormulaValue.FormulaType.Static:
                    EditorGUI.PropertyField(valueRect, staticValueProp, new GUIContent("值"));
                    break;

                case FormulaValue.FormulaType.Reference:
                    DrawReferenceField(valueRect, referencePathProp);
                    break;

                case FormulaValue.FormulaType.Expression:
                    var multiRect = new Rect(position.x + 85, position.y, 60, EditorGUIUtility.singleLineHeight);
                    var opRect = new Rect(position.x + 150, position.y, 70, EditorGUIUtility.singleLineHeight);
                    var offRect = new Rect(position.x + 225, position.y, 60, EditorGUIUtility.singleLineHeight);

                    multiplierProp.floatValue = EditorGUI.FloatField(multiRect, multiplierProp.floatValue);
                    EditorGUI.PropertyField(opRect, operatorTypeProp, GUIContent.none);
                    offsetProp.floatValue = EditorGUI.FloatField(offRect, offsetProp.floatValue);

                    var refRect = new Rect(position.x + 85, position.y + EditorGUIUtility.singleLineHeight + 2, position.width - 85, EditorGUIUtility.singleLineHeight);
                    DrawReferenceField(refRect, referencePathProp);
                    break;

                case FormulaValue.FormulaType.Custom:
                    customFormulaProp.stringValue = EditorGUI.TextField(valueRect, customFormulaProp.stringValue);
                    break;
            }

            EditorGUI.EndProperty();
        }

        private void DrawReferenceField(Rect rect, SerializedProperty pathProp)
        {
            // 显示路径提示
            EditorGUI.LabelField(rect, "引用路径 (如 Runtime.MaxHealth)");
            var pathRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2, rect.width, EditorGUIUtility.singleLineHeight);
            pathProp.stringValue = EditorGUI.TextField(pathRect, pathProp.stringValue);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var type = (FormulaValue.FormulaType)property.FindPropertyRelative("formulaType").enumValueIndex;

            if (type == FormulaValue.FormulaType.Expression)
                return EditorGUIUtility.singleLineHeight * 3 + 4;

            if (type == FormulaValue.FormulaType.Reference)
                return EditorGUIUtility.singleLineHeight * 3 + 4;

            return EditorGUIUtility.singleLineHeight + 2;
        }
    }
}
#endif