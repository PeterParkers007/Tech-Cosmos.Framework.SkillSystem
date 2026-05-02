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
    public class SkillDataSOEditorWindow : EditorWindow
    {
        private SkillDataSO currentTarget;
        private Vector2 scrollPos;
        private Dictionary<string, bool> foldoutStates = new();
        private SerializedObject serializedObject;
        private SerializedProperty serializedDataProp;
        private double lastRepaintTime;
        private const double REPAINT_INTERVAL = 0.05; // 每秒20帧够了
        // 缓存
        private HashSet<string> cachedGeneratedKeys;
        private List<(Type type, DataEntryTypeAttribute attr)> cachedDataEntryTypes;
        private Dictionary<string, List<PropertyInfo>> cachedPropGroups;
        private int lastTargetHash;
        private bool dirty = true;

        private static readonly Color HeaderColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color SectionColor = new Color(0.2f, 0.25f, 0.3f);
        private static readonly Color AccentColor = new Color(0.4f, 0.7f, 1f);

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Selection.selectionChanged += OnSelectionChange;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Selection.selectionChanged -= OnSelectionChange;
            AutoSave();
        }

        private void OnBeforeAssemblyReload() => AutoSave();

        private void OnAfterAssemblyReload()
        {
            // 延迟一帧执行，确保 Unity 完全加载完毕
            EditorApplication.delayCall += () =>
            {
                if (currentTarget != null)
                {
                    var path = AssetDatabase.GetAssetPath(currentTarget);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var fresh = AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);
                        if (fresh != null)
                        {
                            SetTarget(fresh);
                            return;
                        }
                    }
                    SetTarget(null);
                }
                Repaint();
            };
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) AutoSave();
        }

        private void AutoSave()
        {
            if (currentTarget == null) return;
            EditorUtility.SetDirty(currentTarget);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Skill Editor Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<SkillDataSOEditorWindow>("技能编辑器");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        [MenuItem("Tech-Cosmos/SkillSystem/Open Skill Editor", true)]
        private static bool OpenWindowValidate() => Selection.activeObject is SkillDataSO;

        [MenuItem("Tech-Cosmos/SkillSystem/Open Skill Editor", priority = 5)]
        public static void OpenFromSelection()
        {
            var so = Selection.activeObject as SkillDataSO;
            if (so == null) return;
            var window = GetWindow<SkillDataSOEditorWindow>("技能编辑器");
            window.SetTarget(so);
            window.Show();
        }

        public void SetTarget(SkillDataSO target)
        {
            if (target == null)
            {
                currentTarget = null;
                serializedObject = null;
                serializedDataProp = null;
                ClearCache();
                Repaint();
                return;
            }

            currentTarget = target;
            serializedObject = new SerializedObject(target);
            serializedDataProp = serializedObject.FindProperty("serializedData");
            foldoutStates.Clear();
            ClearCache();
            Repaint();
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject is SkillDataSO so && so != null)
                SetTarget(so);
            else if (currentTarget == null)
                Repaint();
        }

        #region 缓存

        private void ClearCache()
        {
            cachedGeneratedKeys = null;
            cachedDataEntryTypes = null;
            cachedPropGroups = null;
            lastTargetHash = 0;
            dirty = true;
        }

        private bool NeedsRefresh()
        {
            if (currentTarget == null) return true;
            if (dirty) return true;
            int hash = currentTarget.GetInstanceID();
            if (hash != lastTargetHash) return true;
            if (cachedGeneratedKeys == null) return true;
            if (cachedPropGroups == null) return true;
            return false;
        }

        private void RefreshCacheIfNeeded()
        {
            if (!NeedsRefresh()) return;

            lastTargetHash = currentTarget.GetInstanceID();
            cachedGeneratedKeys = currentTarget.GetGeneratedKeys();
            cachedDataEntryTypes = CollectDataEntryTypesRaw();
            cachedPropGroups = BuildPropGroups();
            dirty = false;
        }

        private Dictionary<string, List<PropertyInfo>> BuildPropGroups()
        {
            var groups = new Dictionary<string, List<PropertyInfo>>();
            if (currentTarget == null) return groups;

            var soType = currentTarget.GetType();
            foreach (var prop in soType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                var attrs = prop.GetCustomAttributes(typeof(TooltipAttribute), false);
                if (attrs.Length == 0) continue;

                var tooltipAttr = attrs[0] as TooltipAttribute;
                var tooltip = tooltipAttr?.tooltip ?? "";
                var category = ExtractCategory(tooltip);
                if (string.IsNullOrEmpty(category)) category = "其他";

                if (!groups.ContainsKey(category))
                    groups[category] = new List<PropertyInfo>();
                groups[category].Add(prop);
            }
            return groups;
        }

        private HashSet<string> GetGeneratedKeysCached() => cachedGeneratedKeys ?? new HashSet<string>();
        private Dictionary<string, List<PropertyInfo>> GetPropGroupsCached() => cachedPropGroups ?? new Dictionary<string, List<PropertyInfo>>();
        private List<(Type, DataEntryTypeAttribute)> GetDataEntryTypesCached() => cachedDataEntryTypes ?? new List<(Type, DataEntryTypeAttribute)>();

        #endregion

        void OnGUI()
        {

            DrawToolbar();

            if (currentTarget == null)
            {
                serializedObject = null;
                serializedDataProp = null;

                EditorGUILayout.HelpBox(
                    "选择一个 SkillDataSO 资产\n\n" +
                    "方式一：在 Project 窗口右键 SkillDataSO → Open Skill Editor\n" +
                    "方式二：拖拽 SkillDataSO 到下方的选择框",
                    MessageType.Info);

                EditorGUILayout.Space(10);
                var newTarget = EditorGUILayout.ObjectField("选择技能资产", null, typeof(SkillDataSO), false) as SkillDataSO;
                if (newTarget != null)
                    SetTarget(newTarget);

                return;
            }

            RefreshCacheIfNeeded();
            serializedObject.Update();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            DrawBaseInfo();
            DrawConditions();
            DrawMechanisms();
            DrawCustomProperties();
            DrawDataLayer();

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                dirty = true;
                EditorUtility.SetDirty(currentTarget);
            }
        }

        #region 绘制方法

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("技能编辑器", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            var selectTarget = EditorGUILayout.ObjectField(currentTarget, typeof(SkillDataSO), false, GUILayout.Width(200));
            if (selectTarget != currentTarget && selectTarget is SkillDataSO so)
                SetTarget(so);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                EditorUtility.SetDirty(currentTarget);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(40)))
                CreateNewSkillAsset();

            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewSkillAsset()
        {
            var unitType = currentTarget?.GetUnitType();
            if (unitType == null)
            {
                EditorUtility.DisplayDialog("提示", "请先选择一个技能资产作为模板", "确定");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject("创建新技能", $"New{unitType.Name}Skill", "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;

            var newSo = CreateInstance(currentTarget.GetType()) as SkillDataSO;
            if (newSo != null)
            {
                AssetDatabase.CreateAsset(newSo, path);
                AssetDatabase.SaveAssets();
                SetTarget(newSo);
                EditorGUIUtility.PingObject(newSo);
            }
        }

        private void DrawHeader()
        {
            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, HeaderColor);
            GUILayout.Space(10);
            EditorGUILayout.LabelField(EditorGUIUtility.IconContent("ScriptableObject Icon"), GUILayout.Width(30), GUILayout.Height(30));
            EditorGUILayout.LabelField(currentTarget.SkillName, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"({currentTarget.GetType().Name})", EditorStyles.miniLabel);
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawSectionHeader(string title)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, SectionColor);
            var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = AccentColor } };
            EditorGUILayout.LabelField(title, style);
            EditorGUILayout.EndHorizontal();
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, AccentColor);
        }

        private void DrawBaseInfo()
        {
            DrawSectionHeader("基础信息");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillType"), new GUIContent("技能类型"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TriggerEvent"), new GUIContent("触发事件"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillName"), new GUIContent("技能名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillDescription"), new GUIContent("技能描述"));
            EditorGUILayout.EndVertical();
        }

        private void DrawConditions()
        {
            DrawSectionHeader("条件层");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Conditions"), new GUIContent("条件列表"), true);
        }

        private void DrawMechanisms()
        {
            DrawSectionHeader("机制层");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Mechanisms"), new GUIContent("机制列表"), true);
        }

        private void DrawCustomProperties()
        {
            var groups = GetPropGroupsCached();
            if (groups.Count == 0) return;

            DrawSectionHeader("自定义属性");

            foreach (var group in groups.OrderBy(g => g.Key))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (!foldoutStates.ContainsKey(group.Key)) foldoutStates[group.Key] = true;
                foldoutStates[group.Key] = EditorGUILayout.Foldout(foldoutStates[group.Key], $"▸ {group.Key}", true);

                if (foldoutStates[group.Key])
                {
                    EditorGUI.indentLevel++;
                    foreach (var prop in group.Value)
                    {
                        var attrs = prop.GetCustomAttributes(typeof(TooltipAttribute), false);
                        var tooltipAttr = attrs.Length > 0 ? attrs[0] as TooltipAttribute : null;
                        var displayName = ExtractDisplayName(tooltipAttr?.tooltip ?? "") ?? ObjectNames.NicifyVariableName(prop.Name);

                        try
                        {
                            var value = prop.GetValue(currentTarget);
                            var newValue = DrawField(prop.PropertyType, displayName, value);
                            if (!Equals(value, newValue))
                            {
                                prop.SetValue(currentTarget, newValue);
                                dirty = true;
                            }
                        }
                        catch (Exception e)
                        {
                            EditorGUILayout.HelpBox($"错误: {e.Message}", MessageType.Warning);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawDataLayer()
        {
            DrawSectionHeader("数值层");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var generatedKeys = GetGeneratedKeysCached();

            if (serializedDataProp != null)
            {
                int visibleCount = 0;
                for (int i = 0; i < serializedDataProp.arraySize; i++)
                {
                    var elem = serializedDataProp.GetArrayElementAtIndex(i);
                    var key = elem.FindPropertyRelative("key").stringValue;
                    if (generatedKeys.Contains(key)) continue;
                    visibleCount++;
                    DrawDataEntry(elem, i);
                    if (i < serializedDataProp.arraySize - 1) EditorGUILayout.Space(2);
                }
                if (visibleCount == 0)
                    EditorGUILayout.HelpBox("没有手动添加的数据。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField("快速添加", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Float", GUILayout.Height(24))) { AddEntry("newFloat", new FloatValue()); dirty = true; }
            if (GUILayout.Button("Int", GUILayout.Height(24))) { AddEntry("newInt", new IntValue()); dirty = true; }
            if (GUILayout.Button("String", GUILayout.Height(24))) { AddEntry("newString", new StringValue()); dirty = true; }
            if (GUILayout.Button("Bool", GUILayout.Height(24))) { AddEntry("newBool", new BoolValue()); dirty = true; }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 公式(静态)", GUILayout.Height(24)))
            { AddEntry("formula", new FormulaValue { formulaType = FormulaValue.FormulaType.Static }); dirty = true; }
            if (GUILayout.Button("+ 公式(引用)", GUILayout.Height(24)))
            { AddEntry("ref", new FormulaValue { formulaType = FormulaValue.FormulaType.Reference, multiplier = 1f }); dirty = true; }
            if (GUILayout.Button("+ 公式(自定义)", GUILayout.Height(24)))
            { AddEntry("custom", new FormulaValue { formulaType = FormulaValue.FormulaType.Custom }); dirty = true; }
            EditorGUILayout.EndHorizontal();

            var markedTypes = GetDataEntryTypesCached();
            if (markedTypes.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var item in markedTypes.Take(4))
                {
                    var dn = item.Item2?.DisplayName ?? item.Item1.Name;
                    if (GUILayout.Button($"+ {dn}", GUILayout.Height(24)))
                    {
                        try { AddEntry(item.Item1.Name.ToLower(), new SerializableValue { value = Activator.CreateInstance(item.Item1) }); dirty = true; } catch { }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDataEntry(SerializedProperty element, int index)
        {
            var keyProp = element.FindPropertyRelative("key");
            var containerProp = element.FindPropertyRelative("valueContainer");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.Width(140));

            if (containerProp.managedReferenceValue != null)
            {
                string typeLabel = containerProp.managedReferenceValue.GetType().Name switch
                {
                    nameof(FloatValue) => "Float",
                    nameof(IntValue) => "Int",
                    nameof(StringValue) => "Str",
                    nameof(BoolValue) => "Bool",
                    nameof(FormulaValue) => "Formula",
                    nameof(SerializableValue) => GetTypeLabel(containerProp),
                    _ => "Obj"
                };
                EditorGUILayout.LabelField(typeLabel, EditorStyles.miniLabel, GUILayout.Width(50));

                var valueProp = containerProp.FindPropertyRelative("value");
                switch (containerProp.managedReferenceValue)
                {
                    case FloatValue when valueProp != null:
                        valueProp.floatValue = EditorGUILayout.FloatField(valueProp.floatValue); break;
                    case IntValue when valueProp != null:
                        valueProp.intValue = EditorGUILayout.IntField(valueProp.intValue); break;
                    case StringValue when valueProp != null:
                        valueProp.stringValue = EditorGUILayout.TextField(valueProp.stringValue); break;
                    case BoolValue when valueProp != null:
                        valueProp.boolValue = EditorGUILayout.Toggle(valueProp.boolValue); break;
                    case FormulaValue:
                        DrawFormulaInline(containerProp); break;
                    case SerializableValue when valueProp != null:
                        EditorGUILayout.PropertyField(valueProp, GUIContent.none, true); break;
                }

                if (GUILayout.Button("...", GUILayout.Width(25)))
                    ShowTypeMenu(containerProp);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(18)))
            {
                serializedDataProp.DeleteArrayElementAtIndex(index);
                dirty = true;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawFormulaInline(SerializedProperty containerProp)
        {
            var ft = containerProp.FindPropertyRelative("formulaType");
            var sv = containerProp.FindPropertyRelative("staticValue");
            var rp = containerProp.FindPropertyRelative("referencePath");
            var cf = containerProp.FindPropertyRelative("customFormula");

            switch ((FormulaValue.FormulaType)ft.enumValueIndex)
            {
                case FormulaValue.FormulaType.Static:
                    sv.floatValue = EditorGUILayout.FloatField(sv.floatValue, GUILayout.Width(80)); break;
                case FormulaValue.FormulaType.Reference:
                case FormulaValue.FormulaType.Expression:
                    rp.stringValue = EditorGUILayout.TextField(rp.stringValue, GUILayout.Width(120));
                    sv.floatValue = EditorGUILayout.FloatField(sv.floatValue, GUILayout.Width(50)); break;
                case FormulaValue.FormulaType.Custom:
                    cf.stringValue = EditorGUILayout.TextField(cf.stringValue, GUILayout.Width(120)); break;
            }
        }

        private object DrawField(Type type, string label, object value)
        {
            if (type == typeof(int)) return EditorGUILayout.IntField(label, value != null ? (int)value : 0);
            if (type == typeof(float)) return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);
            if (type == typeof(string)) return EditorGUILayout.TextField(label, value != null ? (string)value : "");
            if (type == typeof(bool)) return EditorGUILayout.Toggle(label, value != null ? (bool)value : false);
            if (type == typeof(Vector2)) return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero);
            if (type == typeof(Vector3)) return EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : Vector3.zero);
            if (type == typeof(Color)) return EditorGUILayout.ColorField(label, value != null ? (Color)value : Color.white);
            if (type.IsEnum) return EditorGUILayout.EnumPopup(label, value as Enum ?? (Enum)Activator.CreateInstance(type));
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = value as System.Collections.IList;
                if (list == null)
                {
                    try { list = Activator.CreateInstance(type) as System.Collections.IList; value = list; }
                    catch { EditorGUILayout.LabelField(label, "null"); return value; }
                }
                if (!foldoutStates.ContainsKey(label)) foldoutStates[label] = true;
                foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], $"{label} ({list.Count})");
                if (foldoutStates[label])
                {
                    EditorGUI.indentLevel++;
                    var elemType = type.GetGenericArguments()[0];
                    for (int i = 0; i < list.Count; i++)
                        list[i] = DrawField(elemType, $"元素 [{i}]", list[i]);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", GUILayout.Width(25)))
                        list.Add(elemType.IsValueType ? Activator.CreateInstance(elemType) : null);
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                }
                return list;
            }

            if (type.IsSerializable && !type.IsAbstract && !type.IsPrimitive && type != typeof(string))
            {
                if (value == null)
                {
                    try { value = Activator.CreateInstance(type); }
                    catch { EditorGUILayout.LabelField(label, "null"); return value; }
                }
                if (!foldoutStates.ContainsKey(label)) foldoutStates[label] = false;
                foldoutStates[label] = EditorGUILayout.Foldout(foldoutStates[label], $"{label} ({type.Name})");
                if (foldoutStates[label])
                {
                    EditorGUI.indentLevel++;
                    foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsInitOnly && !f.IsLiteral))
                    {
                        var fv = f.GetValue(value);
                        f.SetValue(value, DrawField(f.FieldType, ObjectNames.NicifyVariableName(f.Name), fv));
                    }
                    EditorGUI.indentLevel--;
                }
                return value;
            }

            EditorGUILayout.LabelField(label, value?.ToString() ?? "null");
            return value;
        }

        #endregion

        #region 工具方法

        private void AddEntry(string key, ValueContainer container)
        {
            int index = serializedDataProp.arraySize;
            serializedDataProp.InsertArrayElementAtIndex(index);
            var elem = serializedDataProp.GetArrayElementAtIndex(index);
            elem.FindPropertyRelative("key").stringValue = key;
            elem.FindPropertyRelative("valueContainer").managedReferenceValue = container;
            serializedObject.ApplyModifiedProperties();
        }

        private void ShowTypeMenu(SerializedProperty cp)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Float"), false, () => SwitchType(cp, new FloatValue()));
            menu.AddItem(new GUIContent("Int"), false, () => SwitchType(cp, new IntValue()));
            menu.AddItem(new GUIContent("String"), false, () => SwitchType(cp, new StringValue()));
            menu.AddItem(new GUIContent("Bool"), false, () => SwitchType(cp, new BoolValue()));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Formula"), false, () => SwitchType(cp, new FormulaValue()));

            var marked = GetDataEntryTypesCached();
            if (marked.Count > 0)
            {
                menu.AddSeparator("");
                foreach (var item in marked)
                {
                    var dn = item.Item2?.DisplayName ?? item.Item1.Name;
                    var ct = item.Item1;
                    menu.AddItem(new GUIContent($"自定义/{dn}"), false,
                        () => { SwitchType(cp, new SerializableValue { value = Activator.CreateInstance(ct) }); dirty = true; });
                }
            }
            menu.ShowAsContext();
        }

        private void SwitchType(SerializedProperty cp, ValueContainer vc)
        {
            cp.managedReferenceValue = vc;
            cp.serializedObject.ApplyModifiedProperties();
            dirty = true;
        }

        private string GetTypeLabel(SerializedProperty cp)
        {
            var vp = cp.FindPropertyRelative("value");
            return vp?.managedReferenceValue?.GetType().Name ?? "Obj";
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

        private List<(Type type, DataEntryTypeAttribute attr)> CollectDataEntryTypesRaw()
        {
            var r = new List<(Type, DataEntryTypeAttribute)>();
            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.IsDynamic) continue;
                try
                {
                    foreach (var t in a.GetExportedTypes())
                    {
                        if (t.IsAbstract || !t.IsSerializable) continue;
                        if (!t.IsClass && !t.IsValueType && !t.IsEnum) continue;
                        var attrs = t.GetCustomAttributes(typeof(DataEntryTypeAttribute), false);
                        if (attrs.Length > 0)
                            r.Add((t, attrs[0] as DataEntryTypeAttribute));
                    }
                }
                catch { }
            }
            r.Sort((a, b) => string.Compare(
                a.Item2?.DisplayName ?? a.Item1.Name,
                b.Item2?.DisplayName ?? b.Item1.Name, StringComparison.Ordinal));
            return r;
        }

        #endregion
    }
}
#endif