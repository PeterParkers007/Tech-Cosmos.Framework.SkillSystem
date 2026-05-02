#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using TechCosmos.SkillSystem.Runtime;
using System.Reflection;

namespace TechCosmos.SkillSystem.Editor
{
    [CustomEditor(typeof(SkillDataSO), true)]
    public class SkillDataSOEditor : UnityEditor.Editor
    {
        private SerializedProperty serializedDataProp;
        // 在类顶部添加折叠状态缓存
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        // 移除缓存，每次刷新
        private Dictionary<string, List<SerializedProperty>> groupedProperties;
        private List<SerializedProperty> ungroupedProperties;

        void OnEnable()
        {
            serializedDataProp = serializedObject.FindProperty("serializedData");
        }

        /// <summary>
        /// 每次 OnInspectorGUI 时重新收集属性，确保最新
        /// </summary>
        private void RefreshProperties()
        {
            serializedObject.Update();

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
                return tooltip.Substring(end + 2).Trim();

            return tooltip;
        }

        public override void OnInspectorGUI()
        {
            // 每次刷新重新收集属性
            RefreshProperties();

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

        

        /// <summary>
        /// 用反射绘制生成的属性
        /// </summary>
        private void DrawGroupedProperties()
        {
            var soType = target.GetType();
            var props = soType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.GetCustomAttribute<TooltipAttribute>() != null)
                .ToList();

            if (props.Count == 0 && groupedProperties.Count == 0 && ungroupedProperties.Count == 0)
            {
                EditorGUILayout.HelpBox("没有自定义属性。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("自定义属性", EditorStyles.boldLabel);

            // 按分类分组
            var reflectedGroups = new Dictionary<string, List<PropertyInfo>>();
            foreach (var prop in props)
            {
                var tooltip = prop.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "";
                var category = ExtractCategory(tooltip);
                if (string.IsNullOrEmpty(category)) category = "其他";

                if (!reflectedGroups.ContainsKey(category))
                    reflectedGroups[category] = new List<PropertyInfo>();
                reflectedGroups[category].Add(prop);
            }

            foreach (var group in reflectedGroups.OrderBy(g => g.Key))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 用 Foldout 实现折叠
                if (!foldoutStates.ContainsKey(group.Key))
                    foldoutStates[group.Key] = true; // 默认展开

                var titleStyle = new GUIStyle(EditorStyles.foldout);
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);

                foldoutStates[group.Key] = EditorGUILayout.Foldout(
                    foldoutStates[group.Key],
                    $"▸ {group.Key}",
                    true,
                    titleStyle
                );

                if (foldoutStates[group.Key])
                {
                    EditorGUI.indentLevel++;
                    foreach (var prop in group.Value)
                    {
                        var tooltip = prop.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "";
                        var displayName = ExtractDisplayName(tooltip) ?? ObjectNames.NicifyVariableName(prop.Name);

                        try
                        {
                            var value = prop.GetValue(target);
                            var newValue = DrawPropertyField(prop.PropertyType, displayName, value);

                            if (!Equals(value, newValue))
                            {
                                prop.SetValue(target, newValue);
                                EditorUtility.SetDirty(target);
                            }
                        }
                        catch (Exception e)
                        {
                            EditorGUILayout.HelpBox($"读取属性失败: {e.Message}", MessageType.Warning);
                        }
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            // 绘制序列化的分组属性（如果有）
            foreach (var group in groupedProperties.OrderBy(g => g.Key))
            {
                if (group.Value.Count == 0) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (!foldoutStates.ContainsKey(group.Key))
                    foldoutStates[group.Key] = true;

                foldoutStates[group.Key] = EditorGUILayout.Foldout(
                    foldoutStates[group.Key],
                    $"▸ {group.Key}",
                    true
                );

                if (foldoutStates[group.Key])
                {
                    EditorGUI.indentLevel++;
                    foreach (var sp in group.Value)
                    {
                        EditorGUILayout.PropertyField(sp, true);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 根据类型绘制对应的输入字段
        /// </summary>
        private object DrawPropertyField(Type type, string label, object value)
        {
            if (type == typeof(int))
                return EditorGUILayout.IntField(label, value != null ? (int)value : 0);
            if (type == typeof(float))
                return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);
            if (type == typeof(string))
                return EditorGUILayout.TextField(label, value != null ? (string)value : "");
            if (type == typeof(bool))
                return EditorGUILayout.Toggle(label, value != null ? (bool)value : false);
            if (type.IsEnum)
                return EditorGUILayout.EnumPopup(label, value as Enum ?? (Enum)Activator.CreateInstance(type));
            if (type == typeof(Vector2))
                return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero);
            if (type == typeof(Vector3))
                return EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : Vector3.zero);
            if (type == typeof(Color))
                return EditorGUILayout.ColorField(label, value != null ? (Color)value : Color.white);

            // 复杂类型：显示 NotSupported
            EditorGUILayout.LabelField(label, value?.ToString() ?? "null");
            return value;
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
                    if (i < serializedDataProp.arraySize - 1)
                        EditorGUILayout.Space(3);
                }

                if (serializedDataProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox(
                        "数值层为空。\n" +
                        "使用下方按钮添加数据条目。",
                        MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(3);

            // 快速添加按钮
            EditorGUILayout.LabelField("快速添加", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Float", GUILayout.Height(22)))
                AddDataEntry("newFloat", new FloatValue());
            if (GUILayout.Button("Int", GUILayout.Height(22)))
                AddDataEntry("newInt", new IntValue());
            if (GUILayout.Button("String", GUILayout.Height(22)))
                AddDataEntry("newString", new StringValue());
            if (GUILayout.Button("Bool", GUILayout.Height(22)))
                AddDataEntry("newBool", new BoolValue());
            EditorGUILayout.EndHorizontal();

            // 公式添加按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 公式(静态)", GUILayout.Height(22)))
                AddDataEntry("formula", new FormulaValue { formulaType = FormulaValue.FormulaType.Static });
            if (GUILayout.Button("+ 公式(引用)", GUILayout.Height(22)))
                AddDataEntry("ref", new FormulaValue { formulaType = FormulaValue.FormulaType.Reference, multiplier = 1f });
            if (GUILayout.Button("+ 公式(自定义)", GUILayout.Height(22)))
                AddDataEntry("custom", new FormulaValue { formulaType = FormulaValue.FormulaType.Custom });
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

        private void DrawDataEntry(SerializedProperty element, int index)
        {
            var keyProp = element.FindPropertyRelative("key");
            var containerProp = element.FindPropertyRelative("valueContainer");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 第一行：Key + 类型 + 删除
            EditorGUILayout.BeginHorizontal();

            // Key 输入
            EditorGUILayout.LabelField("Key", GUILayout.Width(25));
            keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.Width(120));

            // 类型选择和切换
            if (containerProp.managedReferenceValue != null)
            {
                var containerType = containerProp.managedReferenceValue.GetType();
                string typeLabel = containerType.Name switch
                {
                    nameof(FloatValue) => "Float",
                    nameof(IntValue) => "Int",
                    nameof(StringValue) => "Str",
                    nameof(BoolValue) => "Bool",
                    nameof(FormulaValue) => "Formula",
                    _ => containerType.Name
                };

                // 类型标记
                var typeColor = containerProp.managedReferenceValue switch
                {
                    FloatValue => new Color(0.3f, 0.7f, 1f),
                    IntValue => new Color(0.3f, 1f, 0.5f),
                    StringValue => new Color(1f, 0.8f, 0.3f),
                    BoolValue => new Color(1f, 0.5f, 0.5f),
                    FormulaValue => new Color(1f, 0.4f, 1f),
                    _ => Color.white
                };

                var oldColor = GUI.color;
                GUI.color = typeColor;
                EditorGUILayout.LabelField(typeLabel, EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.color = oldColor;

                // 切换类型按钮
                if (GUILayout.Button("...", GUILayout.Width(25)))
                {
                    ShowTypeSwitchMenu(containerProp);
                }
            }

            // 删除按钮
            var deleteColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(16)))
            {
                serializedDataProp.DeleteArrayElementAtIndex(index);
                GUI.backgroundColor = deleteColor;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = deleteColor;

            EditorGUILayout.EndHorizontal();

            // 第二行：值输入区域
            if (containerProp.managedReferenceValue != null)
            {
                DrawValueInput(containerProp);
            }
            else
            {
                EditorGUILayout.HelpBox("点击 ... 选择类型", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 根据类型绘制值输入
        /// </summary>
        private void DrawValueInput(SerializedProperty containerProp)
        {
            var valueProp = containerProp.FindPropertyRelative("value");

            switch (containerProp.managedReferenceValue)
            {
                case FloatValue:
                    if (valueProp != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("值", GUILayout.Width(25));
                        valueProp.floatValue = EditorGUILayout.FloatField(valueProp.floatValue);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;

                case IntValue:
                    if (valueProp != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("值", GUILayout.Width(25));
                        valueProp.intValue = EditorGUILayout.IntField(valueProp.intValue);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;

                case StringValue:
                    if (valueProp != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("值", GUILayout.Width(25));
                        valueProp.stringValue = EditorGUILayout.TextField(valueProp.stringValue);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;

                case BoolValue:
                    if (valueProp != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("值", GUILayout.Width(25));
                        valueProp.boolValue = EditorGUILayout.Toggle(valueProp.boolValue);
                        EditorGUILayout.EndHorizontal();
                    }
                    break;

                case FormulaValue:
                    DrawFormulaInput(containerProp);
                    break;
            }
        }

        /// <summary>
        /// 绘制公式输入
        /// </summary>
        private void DrawFormulaInput(SerializedProperty containerProp)
        {
            var formulaTypeProp = containerProp.FindPropertyRelative("formulaType");
            var staticValueProp = containerProp.FindPropertyRelative("staticValue");
            var referencePathProp = containerProp.FindPropertyRelative("referencePath");
            var multiplierProp = containerProp.FindPropertyRelative("multiplier");
            var offsetProp = containerProp.FindPropertyRelative("offset");
            var operatorTypeProp = containerProp.FindPropertyRelative("operatorType");
            var customFormulaProp = containerProp.FindPropertyRelative("customFormula");

            var formulaType = (FormulaValue.FormulaType)formulaTypeProp.enumValueIndex;

            EditorGUI.indentLevel++;

            // 公式类型选择
            EditorGUILayout.PropertyField(formulaTypeProp, new GUIContent("公式类型"));

            switch (formulaType)
            {
                case FormulaValue.FormulaType.Static:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("静态值", GUILayout.Width(60));
                    staticValueProp.floatValue = EditorGUILayout.FloatField(staticValueProp.floatValue);
                    EditorGUILayout.EndHorizontal();
                    break;

                case FormulaValue.FormulaType.Reference:
                    DrawReferenceFieldWithPreview(referencePathProp);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("操作符", GUILayout.Width(60));

                    var operators = new[] { "Multiply", "Add", "Set" };
                    var opNames = new[] { "× 乘", "+ 加", "= 设" };
                    int opIndex = System.Array.IndexOf(operators, operatorTypeProp.stringValue);
                    if (opIndex < 0) opIndex = 0;

                    int newOpIndex = EditorGUILayout.Popup(opIndex, opNames, GUILayout.Width(70));
                    operatorTypeProp.stringValue = operators[newOpIndex];

                    EditorGUILayout.LabelField("乘数", GUILayout.Width(40));
                    multiplierProp.floatValue = EditorGUILayout.FloatField(multiplierProp.floatValue, GUILayout.Width(60));

                    EditorGUILayout.LabelField("偏移", GUILayout.Width(40));
                    offsetProp.floatValue = EditorGUILayout.FloatField(offsetProp.floatValue, GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                    break;

                case FormulaValue.FormulaType.Expression:
                    EditorGUILayout.LabelField("表达式: 引用值 × 乘数 + 偏移", EditorStyles.miniLabel);

                    DrawReferenceFieldWithPreview(referencePathProp);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("乘数", GUILayout.Width(40));
                    multiplierProp.floatValue = EditorGUILayout.FloatField(multiplierProp.floatValue, GUILayout.Width(60));

                    EditorGUILayout.LabelField("偏移", GUILayout.Width(40));
                    offsetProp.floatValue = EditorGUILayout.FloatField(offsetProp.floatValue, GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                    break;

                case FormulaValue.FormulaType.Custom:
                    EditorGUILayout.LabelField("自定义公式", EditorStyles.miniLabel);
                    EditorGUILayout.HelpBox(
                        "可用变量: caster, target\n" +
                        "示例: caster.Runtime.MaxHealth * 0.5 + 50\n" +
                        "支持: + - * / ( )",
                        MessageType.Info);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("公式", GUILayout.Width(40));
                    customFormulaProp.stringValue = EditorGUILayout.TextField(customFormulaProp.stringValue);
                    EditorGUILayout.EndHorizontal();

                    // 快速插入按钮
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("caster.", EditorStyles.miniButtonLeft))
                        customFormulaProp.stringValue += "caster.";
                    if (GUILayout.Button("target.", EditorStyles.miniButtonMid))
                        customFormulaProp.stringValue += "target.";
                    if (GUILayout.Button(" * ", EditorStyles.miniButtonMid))
                        customFormulaProp.stringValue += " * ";
                    if (GUILayout.Button(" + ", EditorStyles.miniButtonRight))
                        customFormulaProp.stringValue += " + ";
                    EditorGUILayout.EndHorizontal();
                    break;
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 绘制引用路径字段（带常见路径提示）
        /// </summary>
        private void DrawReferenceFieldWithPreview(SerializedProperty pathProp)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("引用路径", GUILayout.Width(60));

            // 输入框
            pathProp.stringValue = EditorGUILayout.TextField(pathProp.stringValue);

            // 快捷选择按钮
            if (GUILayout.Button("▼", GUILayout.Width(25)))
            {
                ShowReferencePathMenu(pathProp);
            }
            EditorGUILayout.EndHorizontal();

            // 路径提示
            if (!string.IsNullOrEmpty(pathProp.stringValue))
            {
                var hintStyle = new GUIStyle(EditorStyles.miniLabel);
                hintStyle.normal.textColor = new Color(0.5f, 0.8f, 0.5f);
                EditorGUILayout.LabelField($"  ↳ 将解析为: {pathProp.stringValue}", hintStyle);
            }
            else
            {
                var hintStyle = new GUIStyle(EditorStyles.miniLabel);
                hintStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                EditorGUILayout.LabelField("  常用路径: caster.Runtime.MaxHealth | target.Runtime.Attack", hintStyle);
            }
        }

        /// <summary>
        /// 显示引用路径快捷菜单（动态扫描 SkillDataField 标记的字段）
        /// </summary>
        private void ShowReferencePathMenu(SerializedProperty pathProp)
        {
            var menu = new GenericMenu();
            var skillDataSO = target as SkillDataSO;
            var unitType = skillDataSO?.GetUnitType();

            // 保存当前属性路径
            var propertyPath = pathProp.propertyPath;
            var targetObject = pathProp.serializedObject.targetObject;

            if (unitType != null)
            {
                CollectFieldsForMenu(unitType, "caster", menu, propertyPath, targetObject);
                menu.AddSeparator("");
                CollectFieldsForMenu(unitType, "target", menu, propertyPath, targetObject);
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("清除路径"), false, () => SetPropertyValue(targetObject, propertyPath, ""));

            menu.ShowAsContext();
        }

        private void CollectFieldsForMenu(Type unitType, string prefix, GenericMenu menu, string propertyPath, UnityEngine.Object targetObject)
        {
            var fields = unitType.GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            bool hasAny = false;

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttributes(typeof(SkillDataFieldAttribute), false)
                    .FirstOrDefault() as SkillDataFieldAttribute;

                if (attr != null)
                {
                    var displayName = attr.DisplayName ?? ObjectNames.NicifyVariableName(field.Name);

                    if (ShouldFlattenInMenu(field.FieldType))
                    {
                        foreach (var subField in field.FieldType.GetFields(
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.Instance))
                        {
                            hasAny = true;
                            var fullPath = $"{prefix}.{field.Name}.{subField.Name}";
                            var display = $"{prefix}/{displayName}/{ObjectNames.NicifyVariableName(subField.Name)}";

                            var capturedPath = fullPath;
                            menu.AddItem(new GUIContent(display), false,
                                () => SetPropertyValue(targetObject, propertyPath, capturedPath));
                        }
                    }
                    else if (IsSimpleType(field.FieldType))
                    {
                        hasAny = true;
                        var fullPath = $"{prefix}.{field.Name}";
                        var display = $"{prefix}/{displayName}";

                        var capturedPath = fullPath;
                        menu.AddItem(new GUIContent(display), false,
                            () => SetPropertyValue(targetObject, propertyPath, capturedPath));
                    }
                }
            }

            if (!hasAny)
            {
                menu.AddDisabledItem(new GUIContent($"{prefix}/(无可用字段)"));
            }
        }

        /// <summary>
        /// 通过 SerializedObject 设置属性值
        /// </summary>
        private void SetPropertyValue(UnityEngine.Object targetObject, string propertyPath, string value)
        {
            var so = new SerializedObject(targetObject);
            var sp = so.FindProperty(propertyPath);
            if (sp != null)
            {
                sp.stringValue = value;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 判断是否需要在菜单中拆分
        /// </summary>
        private bool ShouldFlattenInMenu(Type type)
        {
            if (type.IsPrimitive) return false;
            if (type == typeof(string)) return false;
            if (type.IsEnum) return false;
            if (type.IsArray) return false;
            if (type.IsGenericType) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;

            // 可序列化的结构体/类，有公共字段就拆分
            if (type.IsSerializable)
            {
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                return fields.Length > 0 && fields.Length <= 10;
            }

            return false;
        }

        /// <summary>
        /// 判断是否不适合出现在菜单中的复杂类型
        /// </summary>
        private bool IsComplexTypeForMenu(Type type)
        {
            if (type.IsArray) return true;
            if (type.IsGenericType) return true;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        /// 判断是否是简单类型（数值、字符串等）
        /// </summary>
        private bool IsSimpleType(Type type)
        {
            if (type.IsPrimitive) return true;
            if (type == typeof(string)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(double)) return true;
            if (type.IsEnum) return true;
            if (type == typeof(Vector2)) return true;
            if (type == typeof(Vector3)) return true;
            return false;
        }

        /// <summary>
        /// 显示类型切换菜单
        /// </summary>
        private void ShowTypeSwitchMenu(SerializedProperty containerProp)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Float"), false, () => SwitchValueType(containerProp, new FloatValue()));
            menu.AddItem(new GUIContent("Int"), false, () => SwitchValueType(containerProp, new IntValue()));
            menu.AddItem(new GUIContent("String"), false, () => SwitchValueType(containerProp, new StringValue()));
            menu.AddItem(new GUIContent("Bool"), false, () => SwitchValueType(containerProp, new BoolValue()));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Formula/Static"), false, () => SwitchValueType(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Static }));
            menu.AddItem(new GUIContent("Formula/Reference"), false, () => SwitchValueType(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Reference }));
            menu.AddItem(new GUIContent("Formula/Expression"), false, () => SwitchValueType(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Expression, multiplier = 1f }));
            menu.AddItem(new GUIContent("Formula/Custom"), false, () => SwitchValueType(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Custom }));

            menu.ShowAsContext();
        }

        private void SwitchValueType(SerializedProperty containerProp, ValueContainer newContainer)
        {
            containerProp.managedReferenceValue = newContainer;
            containerProp.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif