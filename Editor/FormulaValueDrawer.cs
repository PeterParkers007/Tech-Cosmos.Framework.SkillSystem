#if UNITY_EDITOR
using TechCosmos.SkillSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.SkillSystem.Editor
{
    [CustomPropertyDrawer(typeof(FormulaValue))]
    public class FormulaValueDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18f;
        private const float PADDING = 2f;

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

            // 第一行：类型选择
            var typeRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
            EditorGUI.PropertyField(typeRect, formulaTypeProp, new GUIContent("公式类型"));

            switch (type)
            {
                case FormulaValue.FormulaType.Static:
                    // 第二行：静态值
                    var staticRect = new Rect(position.x, position.y + LINE_HEIGHT + PADDING, position.width, LINE_HEIGHT);
                    EditorGUI.PropertyField(staticRect, staticValueProp, new GUIContent("静态值"));
                    break;

                case FormulaValue.FormulaType.Reference:
                    // 第二行：引用路径
                    var refPathRect = new Rect(position.x, position.y + LINE_HEIGHT + PADDING, position.width, LINE_HEIGHT);
                    EditorGUI.PropertyField(refPathRect, referencePathProp, new GUIContent("引用路径"));

                    // 第三行：操作符、乘数、偏移
                    var opY = position.y + (LINE_HEIGHT + PADDING) * 2;
                    var opLabelRect = new Rect(position.x, opY, 60, LINE_HEIGHT);
                    var opRect = new Rect(position.x + 60, opY, 80, LINE_HEIGHT);
                    var mulLabelRect = new Rect(position.x + 145, opY, 35, LINE_HEIGHT);
                    var mulRect = new Rect(position.x + 180, opY, 60, LINE_HEIGHT);
                    var offsetLabelRect = new Rect(position.x + 245, opY, 35, LINE_HEIGHT);
                    var offsetRect = new Rect(position.x + 280, opY, position.width - 280, LINE_HEIGHT);

                    EditorGUI.LabelField(opLabelRect, "操作");
                    // 简化操作符显示
                    var ops = new[] { "Multiply", "Add", "Set" };
                    var opNames = new[] { "乘", "加", "设" };
                    int opIdx = System.Array.IndexOf(ops, operatorTypeProp.stringValue);
                    if (opIdx < 0) opIdx = 0;
                    opIdx = EditorGUI.Popup(opRect, opIdx, opNames);
                    operatorTypeProp.stringValue = ops[opIdx];

                    EditorGUI.LabelField(mulLabelRect, "乘数");
                    multiplierProp.floatValue = EditorGUI.FloatField(mulRect, multiplierProp.floatValue);
                    EditorGUI.LabelField(offsetLabelRect, "偏移");
                    offsetProp.floatValue = EditorGUI.FloatField(offsetRect, offsetProp.floatValue);
                    break;

                case FormulaValue.FormulaType.Expression:
                    // 第二行：引用路径
                    var expRefRect = new Rect(position.x, position.y + LINE_HEIGHT + PADDING, position.width, LINE_HEIGHT);
                    EditorGUI.PropertyField(expRefRect, referencePathProp, new GUIContent("引用路径"));

                    // 第三行：乘数、偏移
                    var expY = position.y + (LINE_HEIGHT + PADDING) * 2;
                    var expMulLabelRect = new Rect(position.x, expY, 60, LINE_HEIGHT);
                    var expMulRect = new Rect(position.x + 60, expY, 80, LINE_HEIGHT);
                    var expOffLabelRect = new Rect(position.x + 145, expY, 35, LINE_HEIGHT);
                    var expOffRect = new Rect(position.x + 180, expY, position.width - 180, LINE_HEIGHT);

                    EditorGUI.LabelField(expMulLabelRect, "乘数");
                    multiplierProp.floatValue = EditorGUI.FloatField(expMulRect, multiplierProp.floatValue);
                    EditorGUI.LabelField(expOffLabelRect, "偏移");
                    offsetProp.floatValue = EditorGUI.FloatField(expOffRect, offsetProp.floatValue);
                    break;

                case FormulaValue.FormulaType.Custom:
                    // 第二行：自定义公式
                    var customRect = new Rect(position.x, position.y + LINE_HEIGHT + PADDING, position.width, LINE_HEIGHT);
                    EditorGUI.PropertyField(customRect, customFormulaProp, new GUIContent("自定义公式"));
                    break;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var type = (FormulaValue.FormulaType)property.FindPropertyRelative("formulaType").enumValueIndex;

            switch (type)
            {
                case FormulaValue.FormulaType.Static:
                case FormulaValue.FormulaType.Custom:
                    // 类型选择 + 一个字段行
                    return (LINE_HEIGHT + PADDING) * 2;

                case FormulaValue.FormulaType.Reference:
                case FormulaValue.FormulaType.Expression:
                    // 类型选择 + 引用路径 + 操作行
                    return (LINE_HEIGHT + PADDING) * 3;

                default:
                    return LINE_HEIGHT + PADDING;
            }
        }
    }
}
#endif