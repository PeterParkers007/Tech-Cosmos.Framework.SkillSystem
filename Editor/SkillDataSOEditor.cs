#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TechCosmos.SkillSystem.Runtime;
using static UnityEngine.GraphicsBuffer;

namespace TechCosmos.SkillSystem.Editor
{
    [CustomEditor(typeof(SkillDataSO), true)]
    public class SkillDataSOEditor : UnityEditor.Editor
    {
        private SerializedProperty serializedDataProp;
        private Dictionary<string, List<SerializedProperty>> groupedProperties;
        private List<SerializedProperty> ungroupedProperties;
        private Dictionary<string, bool> foldoutStates = new();

        void OnEnable()
        {
            serializedDataProp = serializedObject.FindProperty("serializedData");
        }

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
                    if (IsSkippedProperty(property.name)) continue;

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
                   name == "SkillType" || name == "TriggerEvent" ||
                   name == "SkillName" || name == "SkillDescription" ||
                   name == "Conditions" || name == "Mechanisms" ||
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
            RefreshProperties();
            serializedObject.Update();

            DrawBaseProperties();
            EditorGUILayout.Space(5);
            DrawConditionsAndMechanisms();
            EditorGUILayout.Space(5);
            DrawGroupedProperties();
            EditorGUILayout.Space(10);
            DrawDataLayer();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorUtility.SetDirty(target);
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
            var c = serializedObject.FindProperty("Conditions");
            var m = serializedObject.FindProperty("Mechanisms");

            if (c != null)
            {
                EditorGUILayout.LabelField("条件层 (Conditions)", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(c, new GUIContent("条件列表"), true);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            if (m != null)
            {
                EditorGUILayout.LabelField("机制层 (Mechanisms)", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(m, new GUIContent("机制列表"), true);
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

            if (props.Count > 0)
            {
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

                    if (!foldoutStates.ContainsKey(group.Key))
                        foldoutStates[group.Key] = true;

                    var titleStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.4f, 0.7f, 1f) }
                    };

                    foldoutStates[group.Key] = EditorGUILayout.Foldout(
                        foldoutStates[group.Key], $"▸ {group.Key}", true, titleStyle);

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

                                // 直接写回，不比较（因为 List 等引用类型修改内部元素后引用不变）
                                prop.SetValue(target, newValue);
                                if (GUI.changed)
                                    EditorUtility.SetDirty(target);
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
            }

            // 绘制序列化的分组属性
            foreach (var group in groupedProperties.OrderBy(g => g.Key))
            {
                if (group.Value.Count == 0) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (!foldoutStates.ContainsKey(group.Key))
                    foldoutStates[group.Key] = true;

                foldoutStates[group.Key] = EditorGUILayout.Foldout(
                    foldoutStates[group.Key], $"▸ {group.Key}", true);

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
                EditorGUILayout.Space(2);
            }

            // 绘制未分组属性
            if (ungroupedProperties.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("▸ 其他", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var sp in ungroupedProperties)
                {
                    EditorGUILayout.PropertyField(sp, true);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 根据类型绘制对应的输入字段
        /// </summary>
        private object DrawPropertyField(Type type, string label, object value)
        {
            // ===== 基础类型 =====
            if (type == typeof(int))
                return EditorGUILayout.IntField(label, value != null ? (int)value : 0);

            if (type == typeof(float))
                return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);

            if (type == typeof(double))
                return (double)EditorGUILayout.FloatField(label, value != null ? (float)(double)value : 0f);

            if (type == typeof(long))
                return (long)EditorGUILayout.LongField(label, value != null ? (long)value : 0);

            if (type == typeof(string))
                return EditorGUILayout.TextField(label, value != null ? (string)value : "");

            if (type == typeof(bool))
                return EditorGUILayout.Toggle(label, value != null ? (bool)value : false);

            // ===== Unity 基础类型 =====
            if (type == typeof(Vector2))
                return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero);

            if (type == typeof(Vector3))
                return EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : Vector3.zero);

            if (type == typeof(Vector4))
                return EditorGUILayout.Vector4Field(label, value != null ? (Vector4)value : Vector4.zero);

            if (type == typeof(Vector2Int))
                return EditorGUILayout.Vector2IntField(label, value != null ? (Vector2Int)value : Vector2Int.zero);

            if (type == typeof(Vector3Int))
                return EditorGUILayout.Vector3IntField(label, value != null ? (Vector3Int)value : Vector3Int.zero);

            if (type == typeof(Color))
                return EditorGUILayout.ColorField(label, value != null ? (Color)value : Color.white);

            if (type == typeof(Color32))
                return (Color32)EditorGUILayout.ColorField(label, value != null ? (Color32)value : new Color32(255, 255, 255, 255));

            if (type == typeof(Rect))
                return EditorGUILayout.RectField(label, value != null ? (Rect)value : Rect.zero);

            if (type == typeof(RectInt))
                return EditorGUILayout.RectIntField(label, value != null ? (RectInt)value : new RectInt());

            if (type == typeof(Bounds))
                return EditorGUILayout.BoundsField(label, value != null ? (Bounds)value : new Bounds());

            if (type == typeof(BoundsInt))
                return EditorGUILayout.BoundsIntField(label, value != null ? (BoundsInt)value : new BoundsInt());

            if (type == typeof(AnimationCurve))
                return EditorGUILayout.CurveField(label, value != null ? (AnimationCurve)value : AnimationCurve.Linear(0, 0, 1, 1));

            if (type == typeof(Gradient))
                return EditorGUILayout.GradientField(label, value != null ? (Gradient)value : new Gradient());

            if (type == typeof(LayerMask))
                return EditorGUILayout.LayerField(label, value != null ? (int)(LayerMask)value : 0);

            // ===== 枚举 =====
            if (type.IsEnum)
            {
                if (value == null)
                    value = Activator.CreateInstance(type);
                return EditorGUILayout.EnumPopup(label, (Enum)value);
            }

            // ===== UnityEngine.Object 引用 =====
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);

            // ===== 泛型 List =====
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = value as System.Collections.IList;
                var elementType = type.GetGenericArguments()[0];

                if (list == null)
                {
                    // 尝试创建新实例
                    try
                    {
                        list = Activator.CreateInstance(type) as System.Collections.IList;
                        value = list;
                    }
                    catch
                    {
                        EditorGUILayout.LabelField(label, "null (无法创建)");
                        return value;
                    }
                }

                if (!foldoutStates.ContainsKey(label))
                    foldoutStates[label] = true;

                foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], $"{label} (Count: {list.Count})");

                if (foldoutStates[label])
                {
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < list.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        var newElement = DrawPropertyField(elementType, $"元素 [{i}]", list[i]);
                        if (!Equals(list[i], newElement))
                            list[i] = newElement;

                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(16)))
                        {
                            list.RemoveAt(i);
                            GUI.backgroundColor = Color.white;
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                        GUI.backgroundColor = Color.white;

                        EditorGUILayout.EndHorizontal();
                    }

                    // 添加元素按钮
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ 添加元素", GUILayout.Width(80)))
                    {
                        try
                        {
                            object newItem = null;
                            if (elementType.IsValueType)
                                newItem = Activator.CreateInstance(elementType);
                            else if (elementType == typeof(string))
                                newItem = "";
                            list.Add(newItem);
                        }
                        catch
                        {
                            list.Add(null);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }
                return list;
            }

            // ===== 泛型 Dictionary =====
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var dict = value as System.Collections.IDictionary;
                if (dict == null)
                {
                    try { value = Activator.CreateInstance(type); dict = value as System.Collections.IDictionary; }
                    catch { EditorGUILayout.LabelField(label, "null"); return value; }
                }

                EditorGUILayout.LabelField(label, $"Dictionary (Count: {dict.Count})");
                EditorGUI.indentLevel++;
                foreach (System.Collections.DictionaryEntry kvp in dict)
                {
                    EditorGUILayout.LabelField("Key", kvp.Key?.ToString() ?? "null");
                    EditorGUILayout.LabelField("Value", kvp.Value?.ToString() ?? "null");
                }
                EditorGUI.indentLevel--;
                return dict;
            }

            // ===== 可序列化的自定义类型（结构体/类） =====
            if (type.IsSerializable && !type.IsAbstract && !type.IsPrimitive && type != typeof(string))
            {
                if (value == null)
                {
                    try { value = Activator.CreateInstance(type); }
                    catch
                    {
                        EditorGUILayout.LabelField(label, "null (无法创建实例)");
                        return null;
                    }
                }

                if (!foldoutStates.ContainsKey(label))
                    foldoutStates[label] = false;

                foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], $"{label} ({type.Name})");

                if (foldoutStates[label])
                {
                    EditorGUI.indentLevel++;
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(f => !f.IsInitOnly && !f.IsLiteral);

                    foreach (var field in fields)
                    {
                        var fieldValue = field.GetValue(value);
                        var newFieldValue = DrawPropertyField(
                            field.FieldType,
                            ObjectNames.NicifyVariableName(field.Name),
                            fieldValue
                        );

                        if (!Equals(fieldValue, newFieldValue))
                            field.SetValue(value, newFieldValue);
                    }
                    EditorGUI.indentLevel--;
                }
                return value;
            }

            // ===== 兜底 =====
            EditorGUILayout.LabelField(label, value?.ToString() ?? "null");
            return value;
        }

        private void DrawDataLayer()
        {
            EditorGUILayout.LabelField("数值层 (Data)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 收集生成属性的 key，用于过滤
            var generatedKeys = new HashSet<string>();
            var so = target as SkillDataSO;
            if (so != null)
            {
                var soType = so.GetType();
                var props = soType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .Where(p => p.GetCustomAttribute<TooltipAttribute>() != null);
                foreach (var prop in props)
                {
                    // 属性名就是 DataKey（首字母可能大小写不同）
                    generatedKeys.Add(prop.Name);
                    generatedKeys.Add(char.ToLower(prop.Name[0]) + prop.Name.Substring(1));
                    generatedKeys.Add(char.ToUpper(prop.Name[0]) + prop.Name.Substring(1));
                }
            }

            int visibleCount = 0;

            if (serializedDataProp != null)
            {
                for (int i = 0; i < serializedDataProp.arraySize; i++)
                {
                    var element = serializedDataProp.GetArrayElementAtIndex(i);
                    var keyProp = element.FindPropertyRelative("key");

                    // 跳过生成属性的 key
                    if (generatedKeys.Contains(keyProp.stringValue))
                        continue;

                    visibleCount++;
                    DrawDataEntry(element, i);
                    if (i < serializedDataProp.arraySize - 1)
                        EditorGUILayout.Space(3);
                }

                if (visibleCount == 0)
                {
                    EditorGUILayout.HelpBox("数值层为空。使用下方按钮添加数据条目。", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(3);
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
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Key", GUILayout.Width(25));
            keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.Width(120));

            if (containerProp.managedReferenceValue != null)
            {
                string typeLabel = containerProp.managedReferenceValue.GetType().Name switch
                {
                    nameof(FloatValue) => "Float",
                    nameof(IntValue) => "Int",
                    nameof(StringValue) => "Str",
                    nameof(BoolValue) => "Bool",
                    nameof(FormulaValue) => "Formula",
                    _ => "Obj"
                };

                var c = GUI.color;
                GUI.color = typeLabel switch
                {
                    "Float" => new Color(0.3f, 0.7f, 1f),
                    "Int" => new Color(0.3f, 1f, 0.5f),
                    "Str" => new Color(1f, 0.8f, 0.3f),
                    "Bool" => new Color(1f, 0.5f, 0.5f),
                    "Formula" => new Color(1f, 0.4f, 1f),
                    _ => Color.white
                };
                EditorGUILayout.LabelField(typeLabel, EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.color = c;

                if (GUILayout.Button("...", GUILayout.Width(25)))
                    ShowTypeSwitchMenu(containerProp);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(16)))
            {
                serializedDataProp.DeleteArrayElementAtIndex(index);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (containerProp.managedReferenceValue != null)
                DrawValueInput(containerProp);
            else
                EditorGUILayout.HelpBox("点击 ... 选择类型", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawValueInput(SerializedProperty containerProp)
        {
            var valueProp = containerProp.FindPropertyRelative("value");

            switch (containerProp.managedReferenceValue)
            {
                case FloatValue:
                    if (valueProp != null) valueProp.floatValue = EditorGUILayout.FloatField("值", valueProp.floatValue);
                    break;
                case IntValue:
                    if (valueProp != null) valueProp.intValue = EditorGUILayout.IntField("值", valueProp.intValue);
                    break;
                case StringValue:
                    if (valueProp != null) valueProp.stringValue = EditorGUILayout.TextField("值", valueProp.stringValue);
                    break;
                case BoolValue:
                    if (valueProp != null) valueProp.boolValue = EditorGUILayout.Toggle("值", valueProp.boolValue);
                    break;
                case FormulaValue:
                    DrawFormulaInput(containerProp);
                    break;
            }
        }

        private void DrawFormulaInput(SerializedProperty containerProp)
        {
            var ft = containerProp.FindPropertyRelative("formulaType");
            var sv = containerProp.FindPropertyRelative("staticValue");
            var rp = containerProp.FindPropertyRelative("referencePath");
            var mp = containerProp.FindPropertyRelative("multiplier");
            var off = containerProp.FindPropertyRelative("offset");
            var op = containerProp.FindPropertyRelative("operatorType");
            var cf = containerProp.FindPropertyRelative("customFormula");

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(ft, new GUIContent("公式类型"));

            var type = (FormulaValue.FormulaType)ft.enumValueIndex;
            switch (type)
            {
                case FormulaValue.FormulaType.Static:
                    sv.floatValue = EditorGUILayout.FloatField("静态值", sv.floatValue);
                    break;

                case FormulaValue.FormulaType.Reference:
                    DrawReferenceFieldWithPreview(rp);
                    EditorGUILayout.BeginHorizontal();
                    var ops = new[] { "Multiply", "Add", "Set" };
                    var opn = new[] { "× 乘", "+ 加", "= 设" };
                    int oi = Array.IndexOf(ops, op.stringValue);
                    if (oi < 0) oi = 0;
                    op.stringValue = ops[EditorGUILayout.Popup("操作符", oi, opn, GUILayout.Width(120))];
                    mp.floatValue = EditorGUILayout.FloatField("乘数", mp.floatValue);
                    off.floatValue = EditorGUILayout.FloatField("偏移", off.floatValue);
                    EditorGUILayout.EndHorizontal();
                    break;

                case FormulaValue.FormulaType.Expression:
                    DrawReferenceFieldWithPreview(rp);
                    mp.floatValue = EditorGUILayout.FloatField("乘数", mp.floatValue);
                    off.floatValue = EditorGUILayout.FloatField("偏移", off.floatValue);
                    break;

                case FormulaValue.FormulaType.Custom:
                    EditorGUILayout.HelpBox("变量: caster, target\n示例: caster.Runtime.Attack * 1.5 + 50", MessageType.Info);
                    cf.stringValue = EditorGUILayout.TextField("公式", cf.stringValue);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("caster.", EditorStyles.miniButtonLeft)) cf.stringValue += "caster.";
                    if (GUILayout.Button("target.", EditorStyles.miniButtonMid)) cf.stringValue += "target.";
                    if (GUILayout.Button(" * ", EditorStyles.miniButtonMid)) cf.stringValue += " * ";
                    if (GUILayout.Button(" + ", EditorStyles.miniButtonRight)) cf.stringValue += " + ";
                    EditorGUILayout.EndHorizontal();
                    break;
            }
            EditorGUI.indentLevel--;
        }

        private void DrawReferenceFieldWithPreview(SerializedProperty pathProp)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("引用路径", GUILayout.Width(60));
            pathProp.stringValue = EditorGUILayout.TextField(pathProp.stringValue);
            if (GUILayout.Button("▼", GUILayout.Width(25)))
                ShowReferencePathMenu(pathProp);
            EditorGUILayout.EndHorizontal();
        }

        private void ShowReferencePathMenu(SerializedProperty pathProp)
        {
            var menu = new GenericMenu();
            var skillDataSO = target as SkillDataSO;
            var unitType = skillDataSO?.GetUnitType();
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
            var fields = unitType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            bool hasAny = false;

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<SkillDataFieldAttribute>();
                if (attr == null) continue;

                var displayName = attr.DisplayName ?? ObjectNames.NicifyVariableName(field.Name);

                if (ShouldFlattenInMenu(field.FieldType))
                {
                    foreach (var subField in field.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        hasAny = true;
                        var fullPath = $"{prefix}.{field.Name}.{subField.Name}";
                        var display = $"{prefix}/{displayName}/{ObjectNames.NicifyVariableName(subField.Name)}";
                        var captured = fullPath;
                        menu.AddItem(new GUIContent(display), false, () => SetPropertyValue(targetObject, propertyPath, captured));
                    }
                }
                else if (IsSimpleType(field.FieldType))
                {
                    hasAny = true;
                    var fullPath = $"{prefix}.{field.Name}";
                    var display = $"{prefix}/{displayName}";
                    var captured = fullPath;
                    menu.AddItem(new GUIContent(display), false, () => SetPropertyValue(targetObject, propertyPath, captured));
                }
            }
            if (!hasAny)
                menu.AddDisabledItem(new GUIContent($"{prefix}/(无可用字段)"));
        }

        private void SetPropertyValue(UnityEngine.Object targetObject, string propertyPath, string value)
        {
            var so = new SerializedObject(targetObject);
            var sp = so.FindProperty(propertyPath);
            if (sp != null) { sp.stringValue = value; so.ApplyModifiedProperties(); }
        }

        private bool ShouldFlattenInMenu(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsArray || type.IsGenericType) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;
            if (type.IsSerializable)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                return fields.Length > 0 && fields.Length <= 10;
            }
            return false;
        }

        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(float) || type == typeof(int) || type == typeof(bool) || type == typeof(double) || type.IsEnum || type == typeof(Vector2) || type == typeof(Vector3);
        }

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