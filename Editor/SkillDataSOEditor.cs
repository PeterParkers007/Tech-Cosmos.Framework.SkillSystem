#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TechCosmos.SkillSystem.Runtime;
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
            return name == "m_Script" || name == "SkillType" || name == "TriggerEvent" ||
                   name == "SkillName" || name == "SkillDescription" ||
                   name == "Conditions" || name == "Mechanisms" || name == "serializedData";
        }

        private string ExtractCategory(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip)) return null;
            int start = tooltip.IndexOf('[');
            int end = tooltip.IndexOf(']');
            if (start == 0 && end > start) return tooltip.Substring(start + 1, end - start - 1);
            return null;
        }

        private string ExtractDisplayName(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip)) return null;
            int end = tooltip.IndexOf(']');
            if (end > 0 && end < tooltip.Length - 1) return tooltip.Substring(end + 2).Trim();
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
            if (GUI.changed) EditorUtility.SetDirty(target);
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
                var reflectedGroups = new Dictionary<string, List<PropertyInfo>>();
                foreach (var prop in props)
                {
                    var tooltip = prop.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "";
                    var category = ExtractCategory(tooltip);
                    if (string.IsNullOrEmpty(category)) category = "其他";
                    if (!reflectedGroups.ContainsKey(category)) reflectedGroups[category] = new List<PropertyInfo>();
                    reflectedGroups[category].Add(prop);
                }

                foreach (var group in reflectedGroups.OrderBy(g => g.Key))
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    if (!foldoutStates.ContainsKey(group.Key)) foldoutStates[group.Key] = true;

                    var ts = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
                    ts.normal.textColor = new Color(0.4f, 0.7f, 1f);
                    foldoutStates[group.Key] = EditorGUILayout.Foldout(foldoutStates[group.Key], $"▸ {group.Key}", true, ts);

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
                                prop.SetValue(target, newValue);
                                if (GUI.changed) EditorUtility.SetDirty(target);
                            }
                            catch (Exception e) { EditorGUILayout.HelpBox($"错误: {e.Message}", MessageType.Warning); }
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }

            foreach (var group in groupedProperties.OrderBy(g => g.Key))
            {
                if (group.Value.Count == 0) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"▸ {group.Key}", EditorStyles.boldLabel);
                foreach (var sp in group.Value) EditorGUILayout.PropertyField(sp, true);
                EditorGUILayout.EndVertical();
            }
        }

        private object DrawPropertyField(Type type, string label, object value)
        {
            if (type == typeof(int)) return EditorGUILayout.IntField(label, value != null ? (int)value : 0);
            if (type == typeof(float)) return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);
            if (type == typeof(double)) return (double)EditorGUILayout.FloatField(label, value != null ? (float)(double)value : 0f);
            if (type == typeof(long)) return (long)EditorGUILayout.LongField(label, value != null ? (long)value : 0);
            if (type == typeof(string)) return EditorGUILayout.TextField(label, value != null ? (string)value : "");
            if (type == typeof(bool)) return EditorGUILayout.Toggle(label, value != null ? (bool)value : false);
            if (type == typeof(Vector2)) return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero);
            if (type == typeof(Vector3)) return EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : Vector3.zero);
            if (type == typeof(Vector4)) return EditorGUILayout.Vector4Field(label, value != null ? (Vector4)value : Vector4.zero);
            if (type == typeof(Vector2Int)) return EditorGUILayout.Vector2IntField(label, value != null ? (Vector2Int)value : Vector2Int.zero);
            if (type == typeof(Vector3Int)) return EditorGUILayout.Vector3IntField(label, value != null ? (Vector3Int)value : Vector3Int.zero);
            if (type == typeof(Color)) return EditorGUILayout.ColorField(label, value != null ? (Color)value : Color.white);
            if (type == typeof(Rect)) return EditorGUILayout.RectField(label, value != null ? (Rect)value : Rect.zero);
            if (type == typeof(AnimationCurve)) return EditorGUILayout.CurveField(label, value != null ? (AnimationCurve)value : AnimationCurve.Linear(0, 0, 1, 1));
            if (type == typeof(Gradient)) return EditorGUILayout.GradientField(label, value != null ? (Gradient)value : new Gradient());
            if (type == typeof(LayerMask)) return EditorGUILayout.LayerField(label, value != null ? (int)(LayerMask)value : 0);
            if (type.IsEnum) return EditorGUILayout.EnumPopup(label, value as Enum ?? (Enum)Activator.CreateInstance(type));
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = value as System.Collections.IList;
                var elemType = type.GetGenericArguments()[0];
                if (list == null) { try { list = Activator.CreateInstance(type) as System.Collections.IList; value = list; } catch { EditorGUILayout.LabelField(label, "null"); return value; } }
                if (!foldoutStates.ContainsKey(label)) foldoutStates[label] = true;
                foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], $"{label} (Count: {list.Count})");
                if (foldoutStates[label])
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < list.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        var ne = DrawPropertyField(elemType, $"元素 [{i}]", list[i]);
                        if (!Equals(list[i], ne)) list[i] = ne;
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(16))) { list.RemoveAt(i); GUI.backgroundColor = Color.white; EditorGUILayout.EndHorizontal(); break; }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+ 添加元素", GUILayout.Width(80)))
                    {
                        try { list.Add(elemType.IsValueType ? Activator.CreateInstance(elemType) : (elemType == typeof(string) ? "" : null)); } catch { list.Add(null); }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                }
                return list;
            }

            if (type.IsSerializable && !type.IsAbstract && !type.IsPrimitive && type != typeof(string))
            {
                if (value == null) { try { value = Activator.CreateInstance(type); } catch { EditorGUILayout.LabelField(label, "null"); return value; } }
                if (!foldoutStates.ContainsKey(label)) foldoutStates[label] = false;
                foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], $"{label} ({type.Name})");
                if (foldoutStates[label])
                {
                    EditorGUI.indentLevel++;
                    foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsInitOnly && !f.IsLiteral))
                    {
                        var fv = f.GetValue(value);
                        var nfv = DrawPropertyField(f.FieldType, ObjectNames.NicifyVariableName(f.Name), fv);
                        if (!Equals(fv, nfv)) f.SetValue(value, nfv);
                    }
                    EditorGUI.indentLevel--;
                }
                return value;
            }

            EditorGUILayout.LabelField(label, value?.ToString() ?? "null");
            return value;
        }

        private void DrawDataLayer()
        {
            EditorGUILayout.LabelField("数值层 (Data)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var generatedKeys = (target as SkillDataSO)?.GetGeneratedKeys() ?? new HashSet<string>();
            int visibleCount = 0;

            if (serializedDataProp != null)
            {
                for (int i = 0; i < serializedDataProp.arraySize; i++)
                {
                    var elem = serializedDataProp.GetArrayElementAtIndex(i);
                    var key = elem.FindPropertyRelative("key").stringValue;
                    if (generatedKeys.Contains(key)) continue;
                    visibleCount++;
                    DrawDataEntry(elem, i);
                    if (i < serializedDataProp.arraySize - 1) EditorGUILayout.Space(3);
                }
                if (visibleCount == 0) EditorGUILayout.HelpBox("数值层为空。使用下方按钮添加。", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("快速添加", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Float", GUILayout.Height(22))) AddDataEntry("newFloat", new FloatValue());
            if (GUILayout.Button("Int", GUILayout.Height(22))) AddDataEntry("newInt", new IntValue());
            if (GUILayout.Button("String", GUILayout.Height(22))) AddDataEntry("newString", new StringValue());
            if (GUILayout.Button("Bool", GUILayout.Height(22))) AddDataEntry("newBool", new BoolValue());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 公式(静态)", GUILayout.Height(22))) AddDataEntry("formula", new FormulaValue { formulaType = FormulaValue.FormulaType.Static });
            if (GUILayout.Button("+ 公式(引用)", GUILayout.Height(22))) AddDataEntry("ref", new FormulaValue { formulaType = FormulaValue.FormulaType.Reference, multiplier = 1f });
            if (GUILayout.Button("+ 公式(自定义)", GUILayout.Height(22))) AddDataEntry("custom", new FormulaValue { formulaType = FormulaValue.FormulaType.Custom });
            EditorGUILayout.EndHorizontal();

            var markedTypes = CollectDataEntryTypes();
            if (markedTypes.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var item in markedTypes.Take(4))
                {
                    var dn = item.attr?.DisplayName ?? item.type.Name;
                    if (GUILayout.Button($"+ {dn}", GUILayout.Height(22)))
                    {
                        try { AddDataEntry(item.type.Name.ToLower(), new SerializableValue { value = Activator.CreateInstance(item.type) }); } catch { }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddDataEntry(string key, ValueContainer container)
        {
            int index = serializedDataProp.arraySize;
            serializedDataProp.InsertArrayElementAtIndex(index);
            var elem = serializedDataProp.GetArrayElementAtIndex(index);
            elem.FindPropertyRelative("key").stringValue = key;
            elem.FindPropertyRelative("valueContainer").managedReferenceValue = container;
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
                string tl = containerProp.managedReferenceValue.GetType().Name switch
                {
                    nameof(FloatValue) => "Float",
                    nameof(IntValue) => "Int",
                    nameof(StringValue) => "Str",
                    nameof(BoolValue) => "Bool",
                    nameof(FormulaValue) => "Formula",
                    nameof(SerializableValue) => GetStl(containerProp),
                    _ => "Obj"
                };
                var c = GUI.color;
                GUI.color = tl switch { "Float" => new Color(0.3f, 0.7f, 1f), "Int" => new Color(0.3f, 1f, 0.5f), "Str" => new Color(1f, 0.8f, 0.3f), "Bool" => new Color(1f, 0.5f, 0.5f), "Formula" => new Color(1f, 0.4f, 1f), _ => Color.white };
                EditorGUILayout.LabelField(tl, EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.color = c;
                if (GUILayout.Button("...", GUILayout.Width(25))) ShowTypeSwitchMenu(containerProp);
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

        private string GetStl(SerializedProperty cp)
        {
            var vp = cp.FindPropertyRelative("value");
            return vp?.managedReferenceValue?.GetType().Name ?? "Obj";
        }

        private void DrawValueInput(SerializedProperty containerProp)
        {
            var valueProp = containerProp.FindPropertyRelative("value");
            switch (containerProp.managedReferenceValue)
            {
                case FloatValue: if (valueProp != null) valueProp.floatValue = EditorGUILayout.FloatField("值", valueProp.floatValue); break;
                case IntValue: if (valueProp != null) valueProp.intValue = EditorGUILayout.IntField("值", valueProp.intValue); break;
                case StringValue: if (valueProp != null) valueProp.stringValue = EditorGUILayout.TextField("值", valueProp.stringValue); break;
                case BoolValue: if (valueProp != null) valueProp.boolValue = EditorGUILayout.Toggle("值", valueProp.boolValue); break;
                case FormulaValue: DrawFormulaInput(containerProp); break;
                case SerializableValue: if (valueProp != null) EditorGUILayout.PropertyField(valueProp, new GUIContent("值"), true); break;
                default: EditorGUILayout.LabelField("值", containerProp.managedReferenceValue?.GetType().Name ?? "null"); break;
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
            switch ((FormulaValue.FormulaType)ft.enumValueIndex)
            {
                case FormulaValue.FormulaType.Static:
                    sv.floatValue = EditorGUILayout.FloatField("静态值", sv.floatValue); break;
                case FormulaValue.FormulaType.Reference:
                    DrawReferenceFieldWithPreview(rp);
                    var ops = new[] { "Multiply", "Add", "Set" };
                    var opn = new[] { "× 乘", "+ 加", "= 设" };
                    int oi = Array.IndexOf(ops, op.stringValue); if (oi < 0) oi = 0;
                    op.stringValue = ops[EditorGUILayout.Popup("操作符", oi, opn)];
                    mp.floatValue = EditorGUILayout.FloatField("乘数", mp.floatValue);
                    off.floatValue = EditorGUILayout.FloatField("偏移", off.floatValue);
                    break;
                case FormulaValue.FormulaType.Expression:
                    DrawReferenceFieldWithPreview(rp);
                    mp.floatValue = EditorGUILayout.FloatField("乘数", mp.floatValue);
                    off.floatValue = EditorGUILayout.FloatField("偏移", off.floatValue);
                    break;
                case FormulaValue.FormulaType.Custom:
                    EditorGUILayout.HelpBox("变量: caster, target\n示例: caster.Runtime.Attack * 1.5", MessageType.Info);
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
            if (GUILayout.Button("▼", GUILayout.Width(25))) ShowReferencePathMenu(pathProp);
            EditorGUILayout.EndHorizontal();
        }

        private void ShowReferencePathMenu(SerializedProperty pathProp)
        {
            var menu = new GenericMenu();
            var so = target as SkillDataSO;
            var ut = so?.GetUnitType();
            var pp = pathProp.propertyPath;
            var to = pathProp.serializedObject.targetObject;
            if (ut != null)
            {
                CollectFieldsForMenu(ut, "caster", menu, pp, to);
                menu.AddSeparator("");
                CollectFieldsForMenu(ut, "target", menu, pp, to);
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("清除路径"), false, () =>
            {
                var s = new SerializedObject(to); var sp = s.FindProperty(pp); if (sp != null) { sp.stringValue = ""; s.ApplyModifiedProperties(); }
            });
            menu.ShowAsContext();
        }

        private void CollectFieldsForMenu(Type ut, string prefix, GenericMenu menu, string pp, UnityEngine.Object to)
        {
            bool any = false;
            foreach (var f in ut.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attr = f.GetCustomAttribute<SkillDataFieldAttribute>(); if (attr == null) continue;
                var dn = attr.DisplayName ?? ObjectNames.NicifyVariableName(f.Name);
                if (ShouldFlattenInMenu(f.FieldType))
                    foreach (var sf in f.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        any = true; var cap = $"{prefix}.{f.Name}.{sf.Name}";
                        menu.AddItem(new GUIContent($"{prefix}/{dn}/{ObjectNames.NicifyVariableName(sf.Name)}"), false, () => { var s = new SerializedObject(to); var sp = s.FindProperty(pp); if (sp != null) { sp.stringValue = cap; s.ApplyModifiedProperties(); } });
                    }
                else if (IsSimpleType(f.FieldType))
                {
                    any = true; var cap = $"{prefix}.{f.Name}";
                    menu.AddItem(new GUIContent($"{prefix}/{dn}"), false, () => { var s = new SerializedObject(to); var sp = s.FindProperty(pp); if (sp != null) { sp.stringValue = cap; s.ApplyModifiedProperties(); } });
                }
            }
            if (!any) menu.AddDisabledItem(new GUIContent($"{prefix}/(无)"));
        }

        private bool ShouldFlattenInMenu(Type t) => !t.IsPrimitive && t != typeof(string) && !t.IsEnum && !t.IsArray && !t.IsGenericType && !typeof(UnityEngine.Object).IsAssignableFrom(t) && t.IsSerializable;

        private bool IsSimpleType(Type t) => t.IsPrimitive || t == typeof(string) || t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(double) || t.IsEnum || t == typeof(Vector2) || t == typeof(Vector3);

        private void ShowTypeSwitchMenu(SerializedProperty containerProp)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Float"), false, () => Switch(containerProp, new FloatValue()));
            menu.AddItem(new GUIContent("Int"), false, () => Switch(containerProp, new IntValue()));
            menu.AddItem(new GUIContent("String"), false, () => Switch(containerProp, new StringValue()));
            menu.AddItem(new GUIContent("Bool"), false, () => Switch(containerProp, new BoolValue()));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Formula/Static"), false, () => Switch(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Static }));
            menu.AddItem(new GUIContent("Formula/Reference"), false, () => Switch(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Reference }));
            menu.AddItem(new GUIContent("Formula/Expression"), false, () => Switch(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Expression, multiplier = 1f }));
            menu.AddItem(new GUIContent("Formula/Custom"), false, () => Switch(containerProp, new FormulaValue { formulaType = FormulaValue.FormulaType.Custom }));

            var marked = CollectDataEntryTypes();
            if (marked.Count > 0)
            {
                menu.AddSeparator("");
                menu.AddDisabledItem(new GUIContent("自定义类型"));
                foreach (var item in marked)
                {
                    var dn = item.attr?.DisplayName ?? ObjectNames.NicifyVariableName(item.type.Name);
                    var cat = item.attr?.Category ?? "自定义";
                    var ct = item.type;
                    menu.AddItem(new GUIContent($"{cat}/{dn}"), false, () =>
                    {
                        try { Switch(containerProp, new SerializableValue { value = Activator.CreateInstance(ct) }); } catch { }
                    });
                }
            }
            menu.ShowAsContext();
        }

        private void Switch(SerializedProperty cp, ValueContainer vc) { cp.managedReferenceValue = vc; cp.serializedObject.ApplyModifiedProperties(); }

        private List<(Type type, DataEntryTypeAttribute attr)> CollectDataEntryTypes()
        {
            var r = new List<(Type, DataEntryTypeAttribute)>();
            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.IsDynamic) continue;
                try
                {
                    foreach (var t in a.GetExportedTypes())
                    {
                        if (!t.IsClass || t.IsAbstract || !t.IsSerializable) continue;
                        var attrs = t.GetCustomAttributes(typeof(DataEntryTypeAttribute), false);
                        if (attrs.Length > 0)
                        {
                            var at = attrs[0] as DataEntryTypeAttribute;
                            r.Add((t, at));
                        }
                    }
                }
                catch { }
            }

            r.Sort((a, b) =>
            {
                var catA = a.Item2?.Category ?? "";
                var catB = b.Item2?.Category ?? "";
                int cmp = string.Compare(catA, catB, StringComparison.Ordinal);
                if (cmp != 0) return cmp;

                var nameA = a.Item2?.DisplayName ?? a.Item1.Name;
                var nameB = b.Item2?.DisplayName ?? b.Item1.Name;
                return string.Compare(nameA, nameB, StringComparison.Ordinal);
            });

            return r;
        }
    }
}
#endif