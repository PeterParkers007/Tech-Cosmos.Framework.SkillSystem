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
        private const double REPAINT_INTERVAL = 0.05;
        // 缓存
        private HashSet<string> cachedGeneratedKeys;
        private List<(Type type, DataEntryTypeAttribute attr)> cachedDataEntryTypes;
        private Dictionary<string, List<PropertyInfo>> cachedPropGroups;
        private HashSet<string> _requiredKeys;
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
            _requiredKeys = null;
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
            if (_requiredKeys == null) return true;
            return false;
        }

        private void RefreshCacheIfNeeded()
        {
            if (!NeedsRefresh()) return;

            lastTargetHash = currentTarget.GetInstanceID();
            cachedGeneratedKeys = currentTarget.GetGeneratedKeys();
            cachedDataEntryTypes = CollectDataEntryTypesRaw();
            cachedPropGroups = BuildPropGroups();
            _requiredKeys = CollectRequiredKeys();
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

        private HashSet<string> CollectRequiredKeys()
        {
            var keys = new HashSet<string>();

            if (currentTarget?.Conditions != null)
            {
                foreach (var cond in currentTarget.Conditions)
                {
                    if (cond == null) continue;
                    var attrs = cond.GetType().GetCustomAttributes<RequiredDataAttribute>();
                    foreach (var attr in attrs)
                        keys.Add(attr.Key);
                }
            }

            if (currentTarget?.Mechanisms != null)
            {
                foreach (var mech in currentTarget.Mechanisms)
                {
                    if (mech == null) continue;
                    var attrs = mech.GetType().GetCustomAttributes<RequiredDataAttribute>();
                    foreach (var attr in attrs)
                        keys.Add(attr.Key);
                }
            }

            return keys;
        }

        private HashSet<string> GetGeneratedKeysCached() => cachedGeneratedKeys ?? new HashSet<string>();
        private Dictionary<string, List<PropertyInfo>> GetPropGroupsCached() => cachedPropGroups ?? new Dictionary<string, List<PropertyInfo>>();
        private List<(Type, DataEntryTypeAttribute)> GetDataEntryTypesCached() => cachedDataEntryTypes ?? new List<(Type, DataEntryTypeAttribute)>();

        #endregion

        void OnGUI()
        {
            if (currentTarget != null && (currentTarget.GetInstanceID() == 0 || serializedObject == null || serializedObject.targetObject == null))
            {
                currentTarget = null;
                serializedObject = null;
                serializedDataProp = null;
            }

            DrawToolbar();

            if (currentTarget == null)
            {
                serializedObject = null;
                serializedDataProp = null;

                EditorGUILayout.HelpBox(
                    "选择一个 SkillDataSO 资产\n\n" +
                    "方式一：在 Project 窗口右键点击 SkillDataSO → Open Skill Editor\n" +
                    "方式二：拖拽 SkillDataSO 到上方的选择框",
                    MessageType.Info);

                EditorGUILayout.Space(10);
                var newTarget = EditorGUILayout.ObjectField("选择技能资产", null, typeof(SkillDataSO), false) as SkillDataSO;
                if (newTarget != null)
                    SetTarget(newTarget);

                return;
            }

            RefreshCacheIfNeeded();
            serializedObject.Update();

            // 同步必需数据项
            SyncRequiredDataEntries();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            DrawBaseInfo();
            DrawConditions();
            DrawMechanisms();
            DrawCustomProperties();
            DrawResources();
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
            DrawSectionHeader("基本信息");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillType"), new GUIContent("技能类型"));

            // 使用 MaskField 绘制多选枚举
            DrawTriggerEventMaskField();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillName"), new GUIContent("技能名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillDescription"), new GUIContent("技能描述"));

            EditorGUILayout.EndVertical();
        }

        private static Type GetTriggerEventEnumType()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType("TechCosmos.SkillSystem.Runtime.TriggerEventType");
                if (type != null && type.IsEnum) return type;
            }
            return null;
        }

        private void DrawTriggerEventMaskField()
        {
            var triggerEventsProp = serializedObject.FindProperty("TriggerEvents");
            var enumType = GetTriggerEventEnumType();

            if (enumType == null)
            {
                // 如果没有枚举类型，就用默认绘制
                EditorGUILayout.PropertyField(triggerEventsProp, new GUIContent("触发事件列表"), true);
                return;
            }

            var enumNames = System.Enum.GetNames(enumType).Where(n => n != "None").ToArray();

            // 获取当前选中的值
            int mask = 0;
            List<string> currentEvents = new List<string>();
            for (int i = 0; i < triggerEventsProp.arraySize; i++)
            {
                currentEvents.Add(triggerEventsProp.GetArrayElementAtIndex(i).stringValue);
            }

            for (int i = 0; i < enumNames.Length; i++)
            {
                if (currentEvents.Contains(enumNames[i]))
                    mask |= (1 << i);
            }

            // 绘制 MaskField
            int newMask = EditorGUILayout.MaskField("触发事件", mask, enumNames);

            // 更新列表
            if (newMask != mask)
            {
                triggerEventsProp.ClearArray();
                int index = 0;
                for (int i = 0; i < enumNames.Length; i++)
                {
                    if ((newMask & (1 << i)) != 0)
                    {
                        triggerEventsProp.InsertArrayElementAtIndex(index);
                        triggerEventsProp.GetArrayElementAtIndex(index).stringValue = enumNames[i];
                        index++;
                    }
                }
                serializedObject.ApplyModifiedProperties();
            }
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
                foldoutStates[group.Key] = EditorGUILayout.Foldout(foldoutStates[group.Key], $"📁 {group.Key}", true);

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
            var requiredKeys = _requiredKeys ?? new HashSet<string>();

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
            var key = keyProp.stringValue;
            var requiredKeys = _requiredKeys ?? new HashSet<string>();
            var generatedKeys = cachedGeneratedKeys ?? new HashSet<string>();
            bool isLocked = requiredKeys.Contains(key) || generatedKeys.Contains(key);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Key 输入框（锁定的显示描述名）
            if (isLocked)
            {
                var desc = GetRequiredDataDescription(key);
                var displayKey = string.IsNullOrEmpty(desc) ? key : desc;
                EditorGUILayout.LabelField(displayKey, EditorStyles.boldLabel, GUILayout.Width(140));
                EditorGUILayout.LabelField("🔒", GUILayout.Width(20));
            }
            else
            {
                keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.Width(140));
            }

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

                var originalColor = GUI.color;
                GUI.color = typeLabel switch
                {
                    "Float" => new Color(0.3f, 0.7f, 1f),
                    "Int" => new Color(0.3f, 1f, 0.5f),
                    "Str" => new Color(1f, 0.8f, 0.3f),
                    "Bool" => new Color(1f, 0.5f, 0.5f),
                    "Formula" => new Color(1f, 0.4f, 1f),
                    _ => Color.white
                };
                EditorGUILayout.LabelField(typeLabel, EditorStyles.miniLabel, GUILayout.Width(60));
                GUI.color = originalColor;

                if (GUILayout.Button("...", GUILayout.Width(25)))
                    ShowTypeMenu(containerProp);
            }

            // 删除按钮（锁定的不可删除）
            if (!isLocked)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✖", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    serializedDataProp.DeleteArrayElementAtIndex(index);
                    dirty = true;
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // 值编辑区域
            if (containerProp.managedReferenceValue != null)
            {
                // 锁定的 key：显示所属信息
                if (isLocked && requiredKeys.Contains(key))
                {
                    DrawRequiredOwners(key);
                }

                EditorGUI.indentLevel++;

                if (containerProp.managedReferenceValue is FormulaValue)
                {
                    DrawFormulaExpanded(containerProp);
                }
                else
                {
                    var valueProp = containerProp.FindPropertyRelative("value");
                    switch (containerProp.managedReferenceValue)
                    {
                        case FloatValue when valueProp != null:
                            valueProp.floatValue = EditorGUILayout.FloatField("值", valueProp.floatValue);
                            break;
                        case IntValue when valueProp != null:
                            valueProp.intValue = EditorGUILayout.IntField("值", valueProp.intValue);
                            break;
                        case StringValue when valueProp != null:
                            valueProp.stringValue = EditorGUILayout.TextField("值", valueProp.stringValue);
                            break;
                        case BoolValue when valueProp != null:
                            valueProp.boolValue = EditorGUILayout.Toggle("值", valueProp.boolValue);
                            break;
                        case SerializableValue when valueProp != null:
                            EditorGUILayout.PropertyField(valueProp, new GUIContent("值"), true);
                            break;
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFormulaExpanded(SerializedProperty containerProp)
        {
            var ft = containerProp.FindPropertyRelative("formulaType");
            var sv = containerProp.FindPropertyRelative("staticValue");
            var rp = containerProp.FindPropertyRelative("referencePath");
            var mp = containerProp.FindPropertyRelative("multiplier");
            var off = containerProp.FindPropertyRelative("offset");
            var op = containerProp.FindPropertyRelative("operatorType");
            var cf = containerProp.FindPropertyRelative("customFormula");

            EditorGUILayout.PropertyField(ft, new GUIContent("公式类型"));

            var type = (FormulaValue.FormulaType)ft.enumValueIndex;

            switch (type)
            {
                case FormulaValue.FormulaType.Static:
                    sv.floatValue = EditorGUILayout.FloatField("静态值", sv.floatValue);
                    break;

                case FormulaValue.FormulaType.Reference:
                    EditorGUILayout.BeginHorizontal();
                    rp.stringValue = EditorGUILayout.TextField("引用路径", rp.stringValue);
                    if (GUILayout.Button("📁", GUILayout.Width(25)))
                        ShowReferencePathMenu(rp);
                    EditorGUILayout.EndHorizontal();

                    var ops = new[] { "Multiply", "Add", "Set" };
                    var opNames = new[] { "× 乘", "+ 加", "= 设" };
                    int opIdx = System.Array.IndexOf(ops, op.stringValue);
                    if (opIdx < 0) opIdx = 0;
                    opIdx = EditorGUILayout.Popup("操作符", opIdx, opNames);
                    op.stringValue = ops[opIdx];

                    mp.floatValue = EditorGUILayout.FloatField("乘数", mp.floatValue);
                    off.floatValue = EditorGUILayout.FloatField("偏移", off.floatValue);

                    EditorGUILayout.HelpBox("💡 需要多引用值计算？切换到“自定义”类型可获得完整公式编辑能力", MessageType.Info);
                    if (GUILayout.Button("升级为自定义公式"))
                    {
                        string currentPath = rp.stringValue;
                        if (!string.IsNullOrEmpty(currentPath))
                        {
                            string formulaStr = currentPath;
                            if (mp.floatValue != 1f)
                                formulaStr = $"{currentPath} * {mp.floatValue}";
                            if (off.floatValue != 0)
                                formulaStr += $" + {off.floatValue}";
                            cf.stringValue = formulaStr;
                        }
                        ft.enumValueIndex = (int)FormulaValue.FormulaType.Custom;
                        dirty = true;
                    }
                    break;

                case FormulaValue.FormulaType.Expression:
                    EditorGUILayout.BeginHorizontal();
                    rp.stringValue = EditorGUILayout.TextField("引用路径", rp.stringValue);
                    if (GUILayout.Button("📁", GUILayout.Width(25)))
                        ShowReferencePathMenu(rp);
                    EditorGUILayout.EndHorizontal();

                    mp.floatValue = EditorGUILayout.FloatField("乘数", mp.floatValue);
                    off.floatValue = EditorGUILayout.FloatField("偏移", off.floatValue);
                    break;

                case FormulaValue.FormulaType.Custom:
                    DrawCustomFormulaEditor(cf);
                    break;
            }
        }

        private void DrawCustomFormulaEditor(SerializedProperty customFormulaProp)
        {
            if (!foldoutStates.ContainsKey("formula_help")) foldoutStates["formula_help"] = false;
            foldoutStates["formula_help"] = EditorGUILayout.Foldout(foldoutStates["formula_help"], "📐 公式语法帮助");
            if (foldoutStates["formula_help"])
            {
                EditorGUILayout.HelpBox(
                    "变量：caster, target\n" +
                    "引用：caster.Runtime.Attack\n" +
                    "运算符：+  -  *  /  (  )\n" +
                    "示例：caster.Attack * 1.5 + target.MaxHealth * 0.1",
                    MessageType.Info);
            }

            var textStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                font = EditorStyles.standardFont
            };

            float lineHeight = EditorGUIUtility.singleLineHeight;
            int lineCount = Mathf.Max(2, Mathf.Min(6, customFormulaProp.stringValue.Split('\n').Length));
            var formulaRect = EditorGUILayout.GetControlRect(false, lineHeight * lineCount + 6);
            customFormulaProp.stringValue = EditorGUI.TextArea(formulaRect, customFormulaProp.stringValue, textStyle);

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📖 引用", EditorStyles.miniButtonLeft, GUILayout.Height(24)))
                ShowInsertReferenceMenu(customFormulaProp);

            if (GUILayout.Button("⚡ 运算符", EditorStyles.miniButtonMid, GUILayout.Height(24)))
                ShowInsertOperatorMenu(customFormulaProp);

            if (GUILayout.Button("🔢 数值", EditorStyles.miniButtonMid, GUILayout.Height(24)))
                ShowInsertNumberMenu(customFormulaProp);

            if (GUILayout.Button("✔ 检查", EditorStyles.miniButtonRight, GUILayout.Height(24)))
                ShowFormulaCheckPopup(customFormulaProp);

            EditorGUILayout.EndHorizontal();
        }

        private void ShowInsertReferenceMenu(SerializedProperty customFormulaProp)
        {
            var so = currentTarget;
            var ut = so?.GetUnitType();
            if (ut == null) return;

            var menu = new GenericMenu();

            var casterPaths = CollectAllFieldPaths(ut, "caster.", new HashSet<Type>()).OrderBy(p => p).ToList();

            if (casterPaths.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("未找到可用字段"));
                menu.ShowAsContext();
                return;
            }

            var groups = casterPaths
                .Select(p => new
                {
                    FullPath = p,
                    ShortPath = p.Substring("caster.".Length),
                    Category = p.Substring("caster.".Length).Split('.')[0]
                })
                .GroupBy(x => x.Category)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                var categoryName = ObjectNames.NicifyVariableName(group.Key);
                var items = group.ToList();

                if (items.Count == 1)
                {
                    var item = items[0];
                    menu.AddItem(new GUIContent($"caster/{categoryName}"), false, () =>
                    {
                        InsertAtCursor(customFormulaProp, item.FullPath);
                        dirty = true;
                    });
                }
                else
                {
                    foreach (var item in items)
                    {
                        var subPath = item.ShortPath.Substring(item.Category.Length + 1);
                        var displayName = ObjectNames.NicifyVariableName(subPath.Replace(".", "/"));
                        menu.AddItem(new GUIContent($"caster/{categoryName}/{displayName}"), false, () =>
                        {
                            InsertAtCursor(customFormulaProp, item.FullPath);
                            dirty = true;
                        });
                    }
                }
            }

            foreach (var group in groups)
            {
                var categoryName = ObjectNames.NicifyVariableName(group.Key);
                var items = group.ToList();

                if (items.Count == 1)
                {
                    var item = items[0];
                    var targetPath = "target." + item.ShortPath;
                    menu.AddItem(new GUIContent($"target/{categoryName}"), false, () =>
                    {
                        InsertAtCursor(customFormulaProp, targetPath);
                        dirty = true;
                    });
                }
                else
                {
                    foreach (var item in items)
                    {
                        var targetPath = "target." + item.ShortPath;
                        var subPath = item.ShortPath.Substring(item.Category.Length + 1);
                        var displayName = ObjectNames.NicifyVariableName(subPath.Replace(".", "/"));
                        menu.AddItem(new GUIContent($"target/{categoryName}/{displayName}"), false, () =>
                        {
                            InsertAtCursor(customFormulaProp, targetPath);
                            dirty = true;
                        });
                    }
                }
            }

            menu.ShowAsContext();
        }

        private void ShowInsertOperatorMenu(SerializedProperty customFormulaProp)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("  +  加法"), false, () => InsertAtCursor(customFormulaProp, " + "));
            menu.AddItem(new GUIContent("  -  减法"), false, () => InsertAtCursor(customFormulaProp, " - "));
            menu.AddItem(new GUIContent("  *  乘法"), false, () => InsertAtCursor(customFormulaProp, " * "));
            menu.AddItem(new GUIContent("  /  除法"), false, () => InsertAtCursor(customFormulaProp, " / "));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("  (  左括号"), false, () => InsertAtCursor(customFormulaProp, "("));
            menu.AddItem(new GUIContent("  )  右括号"), false, () => InsertAtCursor(customFormulaProp, ")"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("常用数值"), false, () => ShowInsertNumberMenu(customFormulaProp));
            menu.AddItem(new GUIContent("常用组合"), false, () => ShowInsertComboMenu(customFormulaProp));

            menu.ShowAsContext();
        }

        private void ShowInsertNumberMenu(SerializedProperty customFormulaProp)
        {
            var menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("── 小数 ──"));
            menu.AddItem(new GUIContent("  0.01  (1%)"), false, () => InsertAtCursor(customFormulaProp, "0.01"));
            menu.AddItem(new GUIContent("  0.05  (5%)"), false, () => InsertAtCursor(customFormulaProp, "0.05"));
            menu.AddItem(new GUIContent("  0.1   (10%)"), false, () => InsertAtCursor(customFormulaProp, "0.1"));
            menu.AddItem(new GUIContent("  0.25  (25%)"), false, () => InsertAtCursor(customFormulaProp, "0.25"));
            menu.AddItem(new GUIContent("  0.5   (50%)"), false, () => InsertAtCursor(customFormulaProp, "0.5"));
            menu.AddItem(new GUIContent("  0.75  (75%)"), false, () => InsertAtCursor(customFormulaProp, "0.75"));

            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("── 整数 ──"));
            menu.AddItem(new GUIContent("  1"), false, () => InsertAtCursor(customFormulaProp, "1"));
            menu.AddItem(new GUIContent("  10"), false, () => InsertAtCursor(customFormulaProp, "10"));
            menu.AddItem(new GUIContent("  100"), false, () => InsertAtCursor(customFormulaProp, "100"));
            menu.AddItem(new GUIContent("  1000"), false, () => InsertAtCursor(customFormulaProp, "1000"));

            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("── 负值 ──"));
            menu.AddItem(new GUIContent("  -1"), false, () => InsertAtCursor(customFormulaProp, "-1"));
            menu.AddItem(new GUIContent("  -0.5"), false, () => InsertAtCursor(customFormulaProp, "-0.5"));

            menu.ShowAsContext();
        }

        private void ShowInsertComboMenu(SerializedProperty customFormulaProp)
        {
            var menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("── 常用引用组合 ──"));
            menu.AddItem(new GUIContent("  caster.  +  target."), false, () => InsertAtCursor(customFormulaProp, "caster. + target."));
            menu.AddItem(new GUIContent("  caster.  -  target."), false, () => InsertAtCursor(customFormulaProp, "caster. - target."));
            menu.AddItem(new GUIContent("  caster.  *  target."), false, () => InsertAtCursor(customFormulaProp, "caster. * target."));

            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("── 常见公式模板 ──"));
            menu.AddItem(new GUIContent("  攻击力 × 倍率"), false, () => InsertAtCursor(customFormulaProp, "caster.Attack * "));
            menu.AddItem(new GUIContent("  攻击力 - 防御力"), false, () => InsertAtCursor(customFormulaProp, "caster.Attack - target.Defense"));
            menu.AddItem(new GUIContent("  最大生命值百分比"), false, () => InsertAtCursor(customFormulaProp, "target.MaxHealth * 0."));
            menu.AddItem(new GUIContent("  当前生命值百分比"), false, () => InsertAtCursor(customFormulaProp, "target.Health * 0."));

            menu.ShowAsContext();
        }

        private void ShowFormulaCheckPopup(SerializedProperty customFormulaProp)
        {
            string formula = customFormulaProp.stringValue;

            if (string.IsNullOrWhiteSpace(formula))
            {
                EditorUtility.DisplayDialog("公式检查", "公式为空", "确定");
                return;
            }

            var issues = new List<string>();

            int openParens = formula.Count(c => c == '(');
            int closeParens = formula.Count(c => c == ')');
            if (openParens != closeParens)
                issues.Add($"⚠ 括号不匹配（左：{openParens}，右：{closeParens}）");

            var matches = System.Text.RegularExpressions.Regex.Matches(formula, @"\b(caster|target)(\.[\w]+)+\b");
            var uniquePaths = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in matches)
                uniquePaths.Add(m.Value);

            if (uniquePaths.Count > 0)
                issues.Add($"✅ 检测到 {uniquePaths.Count} 个引用路径：\n  " + string.Join("\n  ", uniquePaths));
            else
                issues.Add("💡 未检测到引用路径（纯数值计算）");

            if (System.Text.RegularExpressions.Regex.IsMatch(formula, @"[\+\-\*/]{2,}"))
                issues.Add("⚠ 存在连续运算符");

            string result = string.Join("\n\n", issues);
            EditorUtility.DisplayDialog("公式检查", result, "确定");
        }
        private void DrawResources()
        {
            DrawSectionHeader("资源层 (Resources)");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var resourcesProp = serializedObject.FindProperty("_skillResources");
            EditorGUILayout.PropertyField(resourcesProp, new GUIContent("技能资源"), true);

            EditorGUILayout.EndVertical();
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

        #region 必需数据同步与所属显示

        private void SyncRequiredDataEntries()
        {
            if (currentTarget == null || serializedDataProp == null) return;

            // 冲突检测
            DetectAndReportTypeConflicts();

            var requiredKeys = _requiredKeys ?? new HashSet<string>();
            var existingKeys = new HashSet<string>();

            for (int i = 0; i < serializedDataProp.arraySize; i++)
            {
                var elem = serializedDataProp.GetArrayElementAtIndex(i);
                existingKeys.Add(elem.FindPropertyRelative("key").stringValue);
            }

            // 添加缺失的必需数据
            foreach (var key in requiredKeys)
            {
                if (!existingKeys.Contains(key))
                {
                    AddRequiredEntry(key);
                }
            }

            // 移除不需要的
            for (int i = serializedDataProp.arraySize - 1; i >= 0; i--)
            {
                var elem = serializedDataProp.GetArrayElementAtIndex(i);
                var key = elem.FindPropertyRelative("key").stringValue;

                if (requiredKeys.Contains(key)) continue;
                if (cachedGeneratedKeys?.Contains(key) == true) continue;

                serializedDataProp.DeleteArrayElementAtIndex(i);
            }
        }

        private void DetectAndReportTypeConflicts()
        {
            var keySources = new Dictionary<string, List<(Type Type, Type ExpectedType)>>();

            foreach (var c in currentTarget.Conditions ?? Enumerable.Empty<ConditionBase>())
            {
                if (c == null) continue;
                foreach (var attr in c.GetType().GetCustomAttributes<RequiredDataAttribute>())
                {
                    if (!keySources.ContainsKey(attr.Key))
                        keySources[attr.Key] = new List<(Type, Type)>();
                    keySources[attr.Key].Add((c.GetType(), attr.ValueType));
                }
            }

            foreach (var m in currentTarget.Mechanisms ?? Enumerable.Empty<MechanismBase>())
            {
                if (m == null) continue;
                foreach (var attr in m.GetType().GetCustomAttributes<RequiredDataAttribute>())
                {
                    if (!keySources.ContainsKey(attr.Key))
                        keySources[attr.Key] = new List<(Type, Type)>();
                    keySources[attr.Key].Add((m.GetType(), attr.ValueType));
                }
            }

            foreach (var kv in keySources)
            {
                var types = kv.Value.Select(s => s.ExpectedType).Distinct().ToList();
                if (types.Count > 1)
                {
                    var sources = string.Join(", ", kv.Value.Select(s => $"{s.Type.Name}(期望 {s.ExpectedType.Name})"));
                    Debug.LogError(
                        $"[技能系统] 数据键 '{kv.Key}' 类型冲突！\n" +
                        $"  来源: {sources}");
                }
            }
        }

        private void AddRequiredEntry(string key)
        {
            var allAttrs = new List<RequiredDataAttribute>();

            if (currentTarget.Conditions != null)
                foreach (var c in currentTarget.Conditions)
                    if (c != null)
                        allAttrs.AddRange(c.GetType().GetCustomAttributes<RequiredDataAttribute>()
                            .Where(a => a.Key == key));

            if (currentTarget.Mechanisms != null)
                foreach (var m in currentTarget.Mechanisms)
                    if (m != null)
                        allAttrs.AddRange(m.GetType().GetCustomAttributes<RequiredDataAttribute>()
                            .Where(a => a.Key == key));

            var attr = allAttrs.FirstOrDefault();
            if (attr == null) return;

            ValueContainer container;

            if (attr.IsFormula)
            {
                container = new FormulaValue
                {
                    formulaType = attr.FormulaType,
                    staticValue = attr.StaticValue,
                    referencePath = attr.ReferencePath ?? "",
                    customFormula = attr.CustomFormula ?? "",
                    multiplier = 1f
                };
            }
            else if (attr.ValueType == typeof(float))
                container = new FloatValue { value = float.TryParse(attr.DefaultValue, out var f) ? f : 0f };
            else if (attr.ValueType == typeof(int))
                container = new IntValue { value = int.TryParse(attr.DefaultValue, out var i) ? i : 0 };
            else if (attr.ValueType == typeof(string))
                container = new StringValue { value = attr.DefaultValue ?? "" };
            else if (attr.ValueType == typeof(bool))
                container = new BoolValue { value = bool.TryParse(attr.DefaultValue, out var b) && b };
            else
                container = new FloatValue { value = 0f };

            int index = serializedDataProp.arraySize;
            serializedDataProp.InsertArrayElementAtIndex(index);
            var elem = serializedDataProp.GetArrayElementAtIndex(index);
            elem.FindPropertyRelative("key").stringValue = key;
            elem.FindPropertyRelative("valueContainer").managedReferenceValue = container;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRequiredOwners(string key)
        {
            var owners = new List<(string name, string type)>();

            foreach (var c in currentTarget.Conditions ?? Enumerable.Empty<ConditionBase>())
            {
                if (c == null) continue;
                var attr = c.GetType().GetCustomAttributes<RequiredDataAttribute>()
                    .FirstOrDefault(a => a.Key == key);
                if (attr != null)
                {
                    var typeName = ObjectNames.NicifyVariableName(c.GetType().Name);
                    owners.Add((typeName, "条件"));
                }
            }

            foreach (var m in currentTarget.Mechanisms ?? Enumerable.Empty<MechanismBase>())
            {
                if (m == null) continue;
                var attr = m.GetType().GetCustomAttributes<RequiredDataAttribute>()
                    .FirstOrDefault(a => a.Key == key);
                if (attr != null)
                {
                    var typeName = ObjectNames.NicifyVariableName(m.GetType().Name);
                    owners.Add((typeName, "机制"));
                }
            }

            var desc = GetRequiredDataDescription(key);

            if (owners.Count == 1)
            {
                var o = owners[0];
                var text = $"【{o.name}({o.type})】";
                if (!string.IsNullOrEmpty(desc)) text += $" {desc}";
                EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
            }
            else if (owners.Count > 1)
            {
                var foldoutKey = $"required_owner_{key}";
                if (!foldoutStates.ContainsKey(foldoutKey)) foldoutStates[foldoutKey] = false;

                var label = $"📣 属于 {owners.Count} 个模块";
                if (!string.IsNullOrEmpty(desc)) label += $" ─ {desc}";

                foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], label);

                if (foldoutStates[foldoutKey])
                {
                    foreach (var o in owners)
                    {
                        EditorGUILayout.LabelField($"• {o.name} ({o.type})", EditorStyles.miniLabel);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(desc))
            {
                EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);
            }
        }

        private string GetRequiredDataDescription(string key)
        {
            if (currentTarget == null) return null;

            foreach (var c in currentTarget.Conditions ?? Enumerable.Empty<ConditionBase>())
            {
                if (c == null) continue;
                var attr = c.GetType().GetCustomAttributes<RequiredDataAttribute>()
                    .FirstOrDefault(a => a.Key == key);
                if (attr?.Description != null) return attr.Description;
            }

            foreach (var m in currentTarget.Mechanisms ?? Enumerable.Empty<MechanismBase>())
            {
                if (m == null) continue;
                var attr = m.GetType().GetCustomAttributes<RequiredDataAttribute>()
                    .FirstOrDefault(a => a.Key == key);
                if (attr?.Description != null) return attr.Description;
            }

            return null;
        }

        private string GetKeyFromContainer(SerializedProperty containerProp)
        {
            var entryProp = containerProp.serializedObject.FindProperty(
                containerProp.propertyPath.Replace(".valueContainer", ""));
            return entryProp?.FindPropertyRelative("key")?.stringValue ?? "";
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
            var keyProp = GetKeyFromContainer(cp);
            var allowedTypes = GetAllowedTypesForKey(keyProp);

            bool allowAll = allowedTypes == null || allowedTypes.Length == 0;

            var menu = new GenericMenu();

            if (allowAll || allowedTypes.Contains(typeof(float)))
                menu.AddItem(new GUIContent("Float"), false, () => SwitchType(cp, new FloatValue()));

            if (allowAll || allowedTypes.Contains(typeof(int)))
                menu.AddItem(new GUIContent("Int"), false, () => SwitchType(cp, new IntValue()));

            if (allowAll || allowedTypes.Contains(typeof(string)))
                menu.AddItem(new GUIContent("String"), false, () => SwitchType(cp, new StringValue()));

            if (allowAll || allowedTypes.Contains(typeof(bool)))
                menu.AddItem(new GUIContent("Bool"), false, () => SwitchType(cp, new BoolValue()));

            if (allowAll || allowedTypes.Contains(typeof(FormulaValue)))
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Formula/Static"), false, () => SwitchType(cp, new FormulaValue { formulaType = FormulaValue.FormulaType.Static }));
                menu.AddItem(new GUIContent("Formula/Reference"), false, () => SwitchType(cp, new FormulaValue { formulaType = FormulaValue.FormulaType.Reference }));
                menu.AddItem(new GUIContent("Formula/Expression"), false, () => SwitchType(cp, new FormulaValue { formulaType = FormulaValue.FormulaType.Expression, multiplier = 1f }));
                menu.AddItem(new GUIContent("Formula/Custom"), false, () => SwitchType(cp, new FormulaValue { formulaType = FormulaValue.FormulaType.Custom }));
            }

            var marked = GetDataEntryTypesCached();
            if (marked.Count > 0)
            {
                menu.AddSeparator("");
                foreach (var item in marked)
                {
                    if (!allowAll && !allowedTypes.Contains(item.Item1)) continue;
                    var dn = item.Item2?.DisplayName ?? item.Item1.Name;
                    var ct = item.Item1;
                    menu.AddItem(new GUIContent($"自定义/{dn}"), false,
                        () => { SwitchType(cp, new SerializableValue { value = Activator.CreateInstance(ct) }); dirty = true; });
                }
            }
            menu.ShowAsContext();
        }

        private Type[] GetAllowedTypesForKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            foreach (var c in currentTarget.Conditions ?? Enumerable.Empty<ConditionBase>())
            {
                if (c == null) continue;
                var attr = c.GetType().GetCustomAttributes<RequiredDataAttribute>()
                    .FirstOrDefault(a => a.Key == key);
                if (attr?.AllowedTypes != null) return attr.AllowedTypes;
            }

            foreach (var m in currentTarget.Mechanisms ?? Enumerable.Empty<MechanismBase>())
            {
                if (m == null) continue;
                var attr = m.GetType().GetCustomAttributes<RequiredDataAttribute>()
                    .FirstOrDefault(a => a.Key == key);
                if (attr?.AllowedTypes != null) return attr.AllowedTypes;
            }

            return null;
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

        private void ShowReferencePathMenu(SerializedProperty pathProp)
        {
            var menu = new GenericMenu();
            var so = currentTarget;
            var ut = so?.GetUnitType();
            var pp = pathProp.propertyPath;
            var to = pathProp.serializedObject.targetObject;

            if (ut == null)
            {
                menu.AddDisabledItem(new GUIContent("无法获取 Unit 类型"));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("清空路径"), false, () =>
                {
                    var s = new SerializedObject(to);
                    var sp = s.FindProperty(pp);
                    if (sp != null) { sp.stringValue = ""; s.ApplyModifiedProperties(); }
                });
                menu.ShowAsContext();
                return;
            }

            var casterPaths = CollectAllFieldPaths(ut, "caster.", new HashSet<Type>()).OrderBy(p => p).ToList();

            if (casterPaths.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("未找到可用字段"));
            }
            else
            {
                var groups = casterPaths
                    .Select(p => new
                    {
                        FullPath = p,
                        ShortPath = p.Substring("caster.".Length),
                        Category = p.Substring("caster.".Length).Split('.')[0]
                    })
                    .GroupBy(x => x.Category)
                    .OrderBy(g => g.Key);

                foreach (var group in groups)
                {
                    var categoryName = ObjectNames.NicifyVariableName(group.Key);
                    var items = group.ToList();

                    if (items.Count == 1)
                    {
                        var item = items[0];
                        menu.AddItem(new GUIContent($"caster/{categoryName}"), false, () =>
                        {
                            var s = new SerializedObject(to);
                            var sp = s.FindProperty(pp);
                            if (sp != null) { sp.stringValue = item.FullPath; s.ApplyModifiedProperties(); }
                        });
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            var subPath = item.ShortPath.Substring(item.Category.Length + 1);
                            var displayName = ObjectNames.NicifyVariableName(subPath.Replace(".", "/"));
                            menu.AddItem(new GUIContent($"caster/{categoryName}/{displayName}"), false, () =>
                            {
                                var s = new SerializedObject(to);
                                var sp = s.FindProperty(pp);
                                if (sp != null) { sp.stringValue = item.FullPath; s.ApplyModifiedProperties(); }
                            });
                        }
                    }
                }

                foreach (var group in groups)
                {
                    var categoryName = ObjectNames.NicifyVariableName(group.Key);
                    var items = group.ToList();

                    if (items.Count == 1)
                    {
                        var item = items[0];
                        var targetPath = "target." + item.ShortPath;
                        menu.AddItem(new GUIContent($"target/{categoryName}"), false, () =>
                        {
                            var s = new SerializedObject(to);
                            var sp = s.FindProperty(pp);
                            if (sp != null) { sp.stringValue = targetPath; s.ApplyModifiedProperties(); }
                        });
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            var targetPath = "target." + item.ShortPath;
                            var subPath = item.ShortPath.Substring(item.Category.Length + 1);
                            var displayName = ObjectNames.NicifyVariableName(subPath.Replace(".", "/"));
                            menu.AddItem(new GUIContent($"target/{categoryName}/{displayName}"), false, () =>
                            {
                                var s = new SerializedObject(to);
                                var sp = s.FindProperty(pp);
                                if (sp != null) { sp.stringValue = targetPath; s.ApplyModifiedProperties(); }
                            });
                        }
                    }
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("清空路径"), false, () =>
            {
                var s = new SerializedObject(to);
                var sp = s.FindProperty(pp);
                if (sp != null) { sp.stringValue = ""; s.ApplyModifiedProperties(); }
            });

            menu.ShowAsContext();
        }

        private List<string> CollectAllFieldPaths(Type type, string prefix, HashSet<Type> visited)
        {
            var paths = new List<string>();
            if (type == null || !visited.Add(type)) return paths;

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attr = f.GetCustomAttribute<SkillDataFieldAttribute>();
                if (attr == null) continue;

                if (IsSimpleType(f.FieldType))
                {
                    paths.Add(prefix + f.Name);
                }
                else if (ShouldFlattenSubField(f.FieldType))
                {
                    paths.AddRange(CollectSubFields(f.FieldType, prefix + f.Name + ".", new HashSet<Type>()));
                }
            }
            return paths;
        }

        private List<string> CollectSubFields(Type type, string prefix, HashSet<Type> visited)
        {
            var paths = new List<string>();
            if (type == null || !visited.Add(type)) return paths;

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsInitOnly || f.IsLiteral || f.IsStatic) continue;
                if (f.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                if (IsSimpleType(f.FieldType))
                {
                    paths.Add(prefix + f.Name);
                }
                else if (ShouldFlattenSubField(f.FieldType))
                {
                    paths.AddRange(CollectSubFields(f.FieldType, prefix + f.Name + ".", visited));
                }
            }

            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead) continue;
                if (p.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                if (IsSimpleType(p.PropertyType))
                {
                    paths.Add(prefix + p.Name);
                }
                else if (ShouldFlattenSubField(p.PropertyType))
                {
                    paths.AddRange(CollectSubFields(p.PropertyType, prefix + p.Name + ".", visited));
                }
            }

            return paths;
        }

        private bool ShouldFlattenSubField(Type t)
        {
            if (t.IsPrimitive) return false;
            if (t == typeof(string)) return false;
            if (t.IsEnum) return false;
            if (t.IsArray) return false;
            if (t.IsGenericType) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return false;
            if (t.Namespace != null && t.Namespace.StartsWith("UnityEngine")) return false;
            return t.IsSerializable && !t.IsAbstract;
        }

        private bool IsSimpleType(Type t) => t.IsPrimitive || t == typeof(string) || t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(double) || t.IsEnum || t == typeof(Vector2) || t == typeof(Vector3);

        private void InsertAtCursor(SerializedProperty prop, string text)
        {
            prop.stringValue += text;
            prop.serializedObject.ApplyModifiedProperties();
            dirty = true;
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