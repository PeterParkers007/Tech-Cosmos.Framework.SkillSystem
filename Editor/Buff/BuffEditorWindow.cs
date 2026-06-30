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
    public class BuffEditorWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private BuffDataSO currentTarget;
        private SerializedObject serializedObject;

        private static readonly Color AccentColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color RemoveColor = new Color(1f, 0.4f, 0.4f);

        private int pendingDeleteModifier = -1;
        private int pendingDeleteAction = -1;
        private int pendingDeleteExecuter = -1;
        private int pendingDeleteTag = -1;

        private Dictionary<string, bool> _foldoutStates = new();

        [MenuItem("Tech-Cosmos/SkillSystem/Buff Editor", priority = 11)]
        public static void OpenBuffEditor()
        {
            var window = GetWindow<BuffEditorWindow>("Buff 编辑器");
            window.minSize = new Vector2(500, 600);
            if (Selection.activeObject is BuffDataSO so)
                window.SetTarget(so);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChange;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChange;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnAfterAssemblyReload()
        {
            _modifyEnumSearched = false;
            _cachedModifyEnumType = null;
            _actionEnumSearched = false;
            _cachedActionEnumType = null;
            _tagEnumSearched = false;
            _cachedTagEnumType = null;

            if (currentTarget != null)
            {
                var path = AssetDatabase.GetAssetPath(currentTarget);
                if (!string.IsNullOrEmpty(path))
                {
                    var fresh = AssetDatabase.LoadAssetAtPath<BuffDataSO>(path);
                    if (fresh != null) SetTarget(fresh);
                    else SetTarget(null);
                }
                else SetTarget(null);
            }
            Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (currentTarget != null)
                {
                    var path = AssetDatabase.GetAssetPath(currentTarget);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var fresh = AssetDatabase.LoadAssetAtPath<BuffDataSO>(path);
                        SetTarget(fresh ?? null);
                    }
                    else SetTarget(null);
                }
            }
            Repaint();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is BuffDataSO so && so != currentTarget)
                SetTarget(so);
        }

        public void SetTarget(BuffDataSO target)
        {
            currentTarget = target;
            if (target != null && target.GetInstanceID() != 0)
                serializedObject = new SerializedObject(target);
            else
                serializedObject = null;
            Repaint();
        }

        private void OnGUI()
        {
            ApplyPendingDeletes();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Buff 编辑器", EditorStyles.boldLabel);

            var newTarget = EditorGUILayout.ObjectField(currentTarget, typeof(BuffDataSO), false, GUILayout.Width(200)) as BuffDataSO;
            if (newTarget != currentTarget) SetTarget(newTarget);

            if (currentTarget != null)
            {
                if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    CreateNewBuffAsset();
            }
            EditorGUILayout.EndHorizontal();

            if (currentTarget == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("请选择一个 BuffDataSO 资产\n在 Project 窗口右键资产 → Open Buff Editor", MessageType.Info);
                return;
            }

            serializedObject.Update();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawBaseInfo();
            DrawStackConfig();
            DrawModifiers();
            DrawActions();
            DrawEffects();

            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed) EditorUtility.SetDirty(currentTarget);
        }

        private void ApplyPendingDeletes()
        {
            if (serializedObject == null) return;

            if (pendingDeleteModifier >= 0)
            {
                var list = serializedObject.FindProperty("modifiers");
                if (list != null && pendingDeleteModifier < list.arraySize)
                    list.DeleteArrayElementAtIndex(pendingDeleteModifier);
                serializedObject.ApplyModifiedProperties();
                pendingDeleteModifier = -1;
            }
            if (pendingDeleteAction >= 0)
            {
                var list = serializedObject.FindProperty("actions");
                if (list != null && pendingDeleteAction < list.arraySize)
                    list.DeleteArrayElementAtIndex(pendingDeleteAction);
                serializedObject.ApplyModifiedProperties();
                pendingDeleteAction = -1;
            }
            if (pendingDeleteExecuter >= 0)
            {
                var list = serializedObject.FindProperty("effectExecuters");
                if (list != null && pendingDeleteExecuter < list.arraySize)
                    list.DeleteArrayElementAtIndex(pendingDeleteExecuter);
                serializedObject.ApplyModifiedProperties();
                pendingDeleteExecuter = -1;
            }
            if (pendingDeleteTag >= 0)
            {
                var list = serializedObject.FindProperty("tags");
                if (list != null && pendingDeleteTag < list.arraySize)
                    list.DeleteArrayElementAtIndex(pendingDeleteTag);
                serializedObject.ApplyModifiedProperties();
                pendingDeleteTag = -1;
            }
        }

        private void CreateNewBuffAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("创建 Buff 数据", "NewBuffData", "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;

            var newData = CreateInstance<BuffDataSO>();
            newData.buffName = System.IO.Path.GetFileNameWithoutExtension(path);
            newData.duration = 5f;
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();
            SetTarget(newData);
            EditorGUIUtility.PingObject(newData);
        }

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.25f, 0.3f));
            var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = AccentColor } };
            EditorGUI.LabelField(rect, $"  {title}", style);
            EditorGUILayout.Space(2);
        }

        // ===== 基础信息 =====
        private void DrawBaseInfo()
        {
            DrawSectionHeader("基础信息");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buffName"), new GUIContent("Buff 名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"), new GUIContent("持续时间"));
            DrawTagsField(serializedObject.FindProperty("tags"));
            EditorGUILayout.EndVertical();
        }

        // ===== 标签编辑（带折叠） =====
        private void DrawTagsField(SerializedProperty tagsProp)
        {
            if (tagsProp == null) return;

            var tagEnumType = GetBuffTagEnumType();

            // 确保折叠状态存在
            if (!_foldoutStates.ContainsKey("tags"))
                _foldoutStates["tags"] = false;

            // 获取当前标签列表的摘要
            var tagSummary = new List<string>();
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                tagSummary.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
            }
            string summaryText = tagsProp.arraySize > 0
                ? string.Join(", ", tagSummary)
                : "无标签";

            // 折叠标题行
            EditorGUILayout.BeginHorizontal();
            _foldoutStates["tags"] = EditorGUILayout.Foldout(_foldoutStates["tags"], $"标签 ({tagsProp.arraySize})", true);
            GUILayout.FlexibleSpace();

            var summaryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleRight
            };
            if (!_foldoutStates["tags"])
            {
                EditorGUILayout.LabelField(summaryText, summaryStyle, GUILayout.MaxWidth(250));
            }
            EditorGUILayout.EndHorizontal();

            // 折叠内容
            if (_foldoutStates["tags"])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 绘制已有标签
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    var elem = tagsProp.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginHorizontal();

                    // 用枚举下拉框显示
                    if (tagEnumType != null && Enum.TryParse(tagEnumType, elem.stringValue, out var enumVal))
                    {
                        var newVal = EditorGUILayout.EnumPopup((Enum)enumVal);
                        if (newVal.ToString() != elem.stringValue)
                        {
                            elem.stringValue = newVal.ToString();
                        }
                    }
                    else
                    {
                        // 兜底：如果枚举没找到，用文本输入
                        elem.stringValue = EditorGUILayout.TextField(elem.stringValue);
                    }

                    // 删除按钮
                    GUI.backgroundColor = RemoveColor;
                    if (GUILayout.Button("✕", GUILayout.Width(25)))
                    {
                        pendingDeleteTag = i;
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);

                // 添加按钮
                if (tagEnumType != null)
                {
                    var enumNames = Enum.GetNames(tagEnumType).Where(n => n != "None").ToList();

                    // 收集已添加的标签
                    var existingTags = new HashSet<string>();
                    for (int i = 0; i < tagsProp.arraySize; i++)
                    {
                        existingTags.Add(tagsProp.GetArrayElementAtIndex(i).stringValue);
                    }

                    // 可用的标签（排除已添加的）
                    var availableNames = enumNames.Where(n => !existingTags.Contains(n)).ToList();

                    if (availableNames.Count > 0)
                    {
                        if (GUILayout.Button($"+ 添加标签 ({availableNames.Count} 可用)", GUILayout.Height(22)))
                        {
                            ShowAddTagMenu(tagsProp, availableNames);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("所有标签已添加", MessageType.Info);
                    }
                }
                else
                {
                    // 枚举没找到时，提供手动输入添加
                    if (GUILayout.Button("+ 添加标签（手动输入）", GUILayout.Height(22)))
                    {
                        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "";
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        private void ShowAddTagMenu(SerializedProperty tagsProp, List<string> availableNames)
        {
            var menu = new GenericMenu();

            foreach (var name in availableNames)
            {
                var capturedName = name;
                menu.AddItem(new GUIContent(capturedName), false, () =>
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = capturedName;
                    tagsProp.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // ===== 堆叠设置 =====
        private void DrawStackConfig()
        {
            DrawSectionHeader("堆叠设置");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stackPolicy"), new GUIContent("堆叠策略"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStacks"), new GUIContent("最大层数"));
            EditorGUILayout.EndVertical();
        }

        // ===== 枚举类型缓存 =====
        private Type _cachedModifyEnumType;
        private bool _modifyEnumSearched;
        private Type _cachedActionEnumType;
        private bool _actionEnumSearched;
        private Type _cachedTagEnumType;
        private bool _tagEnumSearched;

        private Type GetBuffModifyEnumType()
        {
            if (!_modifyEnumSearched)
            {
                _modifyEnumSearched = true;
                _cachedModifyEnumType = SkillSystemEnumGenerator.ResolveRuntimeEnumType(SkillSystemEnumKind.BuffModifyType);
            }
            return _cachedModifyEnumType;
        }

        private Type GetBuffActionEnumType()
        {
            if (!_actionEnumSearched)
            {
                _actionEnumSearched = true;
                _cachedActionEnumType = SkillSystemEnumGenerator.ResolveRuntimeEnumType(SkillSystemEnumKind.TriggerEvent);
            }
            return _cachedActionEnumType;
        }

        private Type GetBuffTagEnumType()
        {
            if (!_tagEnumSearched)
            {
                _tagEnumSearched = true;
                _cachedTagEnumType = SkillSystemEnumGenerator.ResolveRuntimeEnumType(SkillSystemEnumKind.BuffTag);
            }
            return _cachedTagEnumType;
        }

        // ===== 属性修改 =====
        private void DrawModifiers()
        {
            DrawSectionHeader("属性修改");
            var list = serializedObject.FindProperty("modifiers");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < list.arraySize; i++)
            {
                var elem = list.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var modifyTypeProp = elem.FindPropertyRelative("modifyType");
                var mode = (ModifierMode)elem.FindPropertyRelative("mode").enumValueIndex;
                var val = GetFormulaDisplay(elem.FindPropertyRelative("formula"));
                string modeStr = mode switch { ModifierMode.Set => "设为", ModifierMode.Add => "增加", _ => "乘以" };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i + 1} {modifyTypeProp.stringValue} → {modeStr} {val}", EditorStyles.boldLabel);
                GUI.backgroundColor = RemoveColor;
                if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(18)))
                    pendingDeleteModifier = i;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                DrawModifyTypeField(modifyTypeProp);
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("mode"), new GUIContent("模式"));
                DrawFormulaValueProperty(elem.FindPropertyRelative("formula"));
                EditorGUILayout.EndVertical();
            }

            var modifyEnum = GetBuffModifyEnumType();
            if (modifyEnum != null)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var name in Enum.GetNames(modifyEnum).Take(4))
                {
                    if (name == "None") continue;
                    if (GUILayout.Button($"+ {name}", GUILayout.Height(22)))
                        AddModifier(list, name, ModifierMode.Multiply, 1f);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ 自定义修改")) AddModifier(list, "NewModifyType", ModifierMode.Multiply, 1f);
            EditorGUILayout.EndVertical();
        }

        private void DrawModifyTypeField(SerializedProperty prop)
        {
            var enumType = GetBuffModifyEnumType();
            if (enumType != null && Enum.TryParse(enumType, prop.stringValue, out var enumVal))
            {
                var newVal = EditorGUILayout.EnumPopup("修改类型", (Enum)enumVal);
                if (newVal.ToString() != prop.stringValue)
                    prop.stringValue = newVal.ToString();
            }
            else
            {
                prop.stringValue = EditorGUILayout.TextField("修改类型", prop.stringValue);
            }
        }

        private void AddModifier(SerializedProperty list, string type, ModifierMode mode, float val)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            var elem = list.GetArrayElementAtIndex(list.arraySize - 1);
            elem.FindPropertyRelative("modifyType").stringValue = type;
            elem.FindPropertyRelative("mode").enumValueIndex = (int)mode;
            var fp = elem.FindPropertyRelative("formula");
            fp.FindPropertyRelative("formulaType").enumValueIndex = (int)BuffFormulaType.Static;
            fp.FindPropertyRelative("staticValue").floatValue = val;
        }

        private string GetFormulaDisplay(SerializedProperty formulaProp)
        {
            var type = (BuffFormulaType)formulaProp.FindPropertyRelative("formulaType").enumValueIndex;
            return type switch
            {
                BuffFormulaType.Static => formulaProp.FindPropertyRelative("staticValue").floatValue.ToString("F2"),
                BuffFormulaType.Reference => $"\"{formulaProp.FindPropertyRelative("referencePath").stringValue}\"",
                BuffFormulaType.Custom => $"\"{formulaProp.FindPropertyRelative("customFormula").stringValue}\"",
                _ => "?"
            };
        }

        // ===== 公式编辑 =====
        private void DrawFormulaValueProperty(SerializedProperty formulaProp)
        {
            var typeProp = formulaProp.FindPropertyRelative("formulaType");
            var staticValueProp = formulaProp.FindPropertyRelative("staticValue");
            var referencePathProp = formulaProp.FindPropertyRelative("referencePath");
            var multiplierProp = formulaProp.FindPropertyRelative("multiplier");
            var offsetProp = formulaProp.FindPropertyRelative("offset");
            var customFormulaProp = formulaProp.FindPropertyRelative("customFormula");

            var type = (BuffFormulaType)typeProp.enumValueIndex;

            EditorGUILayout.PropertyField(typeProp, new GUIContent("公式类型"));

            switch (type)
            {
                case BuffFormulaType.Static:
                    EditorGUILayout.PropertyField(staticValueProp, new GUIContent("静态值"));
                    break;

                case BuffFormulaType.Reference:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(referencePathProp, new GUIContent("引用字段"));
                    if (GUILayout.Button("🔍", GUILayout.Width(25)))
                        ShowReferenceFieldMenu(referencePathProp);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(multiplierProp, new GUIContent("乘数"));
                    EditorGUILayout.PropertyField(offsetProp, new GUIContent("偏移"));
                    break;

                case BuffFormulaType.Custom:
                    DrawCustomFormulaEditor(customFormulaProp);
                    break;
            }
        }

        private void DrawCustomFormulaEditor(SerializedProperty customFormulaProp)
        {
            if (!_foldoutStates.ContainsKey("formula_help")) _foldoutStates["formula_help"] = false;
            _foldoutStates["formula_help"] = EditorGUILayout.Foldout(_foldoutStates["formula_help"], "📃 公式语法帮助");
            if (_foldoutStates["formula_help"])
            {
                EditorGUILayout.HelpBox(
                    "关键字：caster, target\n" +
                    "引用：caster.Runtime.Attack\n" +
                    "运算符：+  -  *  /  (  )\n" +
                    "示例：caster.Runtime.Attack * 1.5 + target.Runtime.Health * 0.1",
                    MessageType.Info);
            }

            var textStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            float lineHeight = EditorGUIUtility.singleLineHeight;
            int lineCount = Mathf.Max(2, Mathf.Min(6, customFormulaProp.stringValue.Split('\n').Length));
            var formulaRect = EditorGUILayout.GetControlRect(false, lineHeight * lineCount + 6);
            customFormulaProp.stringValue = EditorGUI.TextArea(formulaRect, customFormulaProp.stringValue, textStyle);

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 引用", EditorStyles.miniButtonLeft, GUILayout.Height(24)))
                ShowReferenceInsertMenu(customFormulaProp);
            if (GUILayout.Button("⚙ 运算符", EditorStyles.miniButtonMid, GUILayout.Height(24)))
                ShowInsertOperatorMenu(customFormulaProp);
            if (GUILayout.Button("🔢 数值", EditorStyles.miniButtonMid, GUILayout.Height(24)))
                ShowInsertNumberMenu(customFormulaProp);
            if (GUILayout.Button("✓ 检查", EditorStyles.miniButtonRight, GUILayout.Height(24)))
                ShowFormulaCheckPopup(customFormulaProp);
            EditorGUILayout.EndHorizontal();
        }

        private void ShowReferenceFieldMenu(SerializedProperty pathProp)
        {
            var menu = new GenericMenu();

            var targetTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetExportedTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<ApplyBuffTargetAttribute>() != null)
                .ToList();

            if (targetTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有找到标记了 [ApplyBuffTarget] 的类型"));
            }
            else
            {
                foreach (var type in targetTypes)
                {
                    CollectBuffFields(type, "", menu, pathProp);
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("手动输入"), false, () => { });
            menu.ShowAsContext();
        }

        private void CollectBuffFields(Type type, string prefix, GenericMenu menu, SerializedProperty pathProp)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<BuffFieldAttribute>() != null)
                {
                    if (IsSimpleBuffType(field.FieldType))
                    {
                        string path = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
                        menu.AddItem(new GUIContent($"{type.Name}/{path}"), false, () =>
                        {
                            pathProp.stringValue = path;
                            pathProp.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    else
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
                        CollectAllPublicFields(field.FieldType, newPrefix, menu, pathProp);
                    }
                }
                else if (field.FieldType.GetCustomAttribute<BuffFieldAttribute>() != null)
                {
                    string newPrefix = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
                    CollectAllPublicFields(field.FieldType, newPrefix, menu, pathProp);
                }
            }

            // 扫描公开属性
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<BuffFieldAttribute>() != null)
                {
                    if (IsSimpleBuffType(prop.PropertyType))
                    {
                        string path = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                        menu.AddItem(new GUIContent($"{type.Name}/{path}"), false, () =>
                        {
                            pathProp.stringValue = path;
                            pathProp.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    else
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                        CollectAllPublicFields(prop.PropertyType, newPrefix, menu, pathProp);
                    }
                }
                else if (prop.PropertyType.GetCustomAttribute<BuffFieldAttribute>() != null)
                {
                    string newPrefix = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    CollectAllPublicFields(prop.PropertyType, newPrefix, menu, pathProp);
                }
            }
        }

        private void CollectAllPublicFields(Type type, string prefix, GenericMenu menu, SerializedProperty pathProp)
        {
            // 扫描字段
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSimpleBuffType(field.FieldType))
                {
                    string path = $"{prefix}.{field.Name}";
                    menu.AddItem(new GUIContent($"{type.Name}/{path}"), false, () =>
                    {
                        pathProp.stringValue = path;
                        pathProp.serializedObject.ApplyModifiedProperties();
                    });
                }
            }

            // 扫描公开属性
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && IsSimpleBuffType(prop.PropertyType))
                {
                    string path = $"{prefix}.{prop.Name}";
                    menu.AddItem(new GUIContent($"{type.Name}/{path}"), false, () =>
                    {
                        pathProp.stringValue = path;
                        pathProp.serializedObject.ApplyModifiedProperties();
                    });
                }
            }
        }

        private bool IsSimpleBuffType(Type t)
        {
            return t.IsPrimitive || t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(string) || t.IsEnum;
        }

        private void ShowReferenceInsertMenu(SerializedProperty prop)
        {
            var menu = new GenericMenu();

            var targetTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.GetExportedTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<ApplyBuffTargetAttribute>() != null)
                .ToList();

            if (targetTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有找到标记了 [ApplyBuffTarget] 的类型"));
            }
            else
            {
                foreach (var type in targetTypes)
                {
                    CollectFormulaFields(type, "", menu, prop);
                }
            }

            menu.ShowAsContext();
        }

        private void CollectFormulaFields(Type type, string prefix, GenericMenu menu, SerializedProperty prop)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<BuffFieldAttribute>() != null)
                {
                    if (IsSimpleBuffType(field.FieldType))
                    {
                        string path = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
                        menu.AddItem(new GUIContent($"caster/{path}"), false, () => { prop.stringValue += $"caster.{path}"; prop.serializedObject.ApplyModifiedProperties(); });
                        menu.AddItem(new GUIContent($"target/{path}"), false, () => { prop.stringValue += $"target.{path}"; prop.serializedObject.ApplyModifiedProperties(); });
                    }
                    else
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";
                        CollectAllPublicFormulaFields(field.FieldType, newPrefix, menu, prop);
                    }
                }
            }

            // 扫描公开属性
            foreach (var propInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propInfo.GetCustomAttribute<BuffFieldAttribute>() != null)
                {
                    if (IsSimpleBuffType(propInfo.PropertyType))
                    {
                        string path = string.IsNullOrEmpty(prefix) ? propInfo.Name : $"{prefix}.{propInfo.Name}";
                        menu.AddItem(new GUIContent($"caster/{path}"), false, () => { prop.stringValue += $"caster.{path}"; prop.serializedObject.ApplyModifiedProperties(); });
                        menu.AddItem(new GUIContent($"target/{path}"), false, () => { prop.stringValue += $"target.{path}"; prop.serializedObject.ApplyModifiedProperties(); });
                    }
                    else
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? propInfo.Name : $"{prefix}.{propInfo.Name}";
                        CollectAllPublicFormulaFields(propInfo.PropertyType, newPrefix, menu, prop);
                    }
                }
            }
        }

        private void CollectAllPublicFormulaFields(Type type, string prefix, GenericMenu menu, SerializedProperty prop)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsSimpleBuffType(field.FieldType))
                {
                    string path = $"{prefix}.{field.Name}";
                    menu.AddItem(new GUIContent($"caster/{path}"), false, () =>
                    {
                        prop.stringValue += $"caster.{path}";
                        prop.serializedObject.ApplyModifiedProperties();
                    });
                    menu.AddItem(new GUIContent($"target/{path}"), false, () =>
                    {
                        prop.stringValue += $"target.{path}";
                        prop.serializedObject.ApplyModifiedProperties();
                    });
                }
            }

            // 扫描公开属性
            foreach (var propInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propInfo.CanRead && IsSimpleBuffType(propInfo.PropertyType))
                {
                    string path = $"{prefix}.{propInfo.Name}";
                    menu.AddItem(new GUIContent($"caster/{path}"), false, () =>
                    {
                        prop.stringValue += $"caster.{path}";
                        prop.serializedObject.ApplyModifiedProperties();
                    });
                    menu.AddItem(new GUIContent($"target/{path}"), false, () =>
                    {
                        prop.stringValue += $"target.{path}";
                        prop.serializedObject.ApplyModifiedProperties();
                    });
                }
            }
        }

        private void ShowInsertOperatorMenu(SerializedProperty prop)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("  +  加法"), false, () => { prop.stringValue += " + "; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  -  减法"), false, () => { prop.stringValue += " - "; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  *  乘法"), false, () => { prop.stringValue += " * "; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  /  除法"), false, () => { prop.stringValue += " / "; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("  (  左括号"), false, () => { prop.stringValue += "("; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  )  右括号"), false, () => { prop.stringValue += ")"; prop.serializedObject.ApplyModifiedProperties(); });
            menu.ShowAsContext();
        }

        private void ShowInsertNumberMenu(SerializedProperty prop)
        {
            var menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent("── 小数 ──"));
            menu.AddItem(new GUIContent("  0.01 (1%)"), false, () => { prop.stringValue += "0.01"; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  0.1  (10%)"), false, () => { prop.stringValue += "0.1"; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  0.5  (50%)"), false, () => { prop.stringValue += "0.5"; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("── 整数 ──"));
            menu.AddItem(new GUIContent("  1"), false, () => { prop.stringValue += "1"; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  10"), false, () => { prop.stringValue += "10"; prop.serializedObject.ApplyModifiedProperties(); });
            menu.AddItem(new GUIContent("  100"), false, () => { prop.stringValue += "100"; prop.serializedObject.ApplyModifiedProperties(); });
            menu.ShowAsContext();
        }

        private void ShowFormulaCheckPopup(SerializedProperty prop)
        {
            string formula = prop.stringValue;
            if (string.IsNullOrWhiteSpace(formula))
            {
                EditorUtility.DisplayDialog("公式检查", "公式为空", "确定");
                return;
            }

            var issues = new List<string>();
            int open = formula.Count(c => c == '(');
            int close = formula.Count(c => c == ')');
            if (open != close)
                issues.Add($"⚠ 括号不匹配（左：{open}，右：{close}）");

            var matches = System.Text.RegularExpressions.Regex.Matches(formula, @"\b(caster|target)\.[\w.]+");
            var paths = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match m in matches)
                paths.Add(m.Value);

            if (paths.Count > 0)
                issues.Add($"✓ 检测到 {paths.Count} 个引用路径：\n  " + string.Join("\n  ", paths));
            else
                issues.Add("ℹ 未检测到引用路径（纯数值计算）");

            EditorUtility.DisplayDialog("公式检查", string.Join("\n\n", issues), "确定");
        }

        // ===== 事件响应 =====
        private void DrawActions()
        {
            DrawSectionHeader("事件响应");
            var list = serializedObject.FindProperty("actions");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < list.arraySize; i++)
            {
                var elem = list.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i + 1} {elem.FindPropertyRelative("actionName").stringValue}", EditorStyles.boldLabel);
                GUI.backgroundColor = RemoveColor;
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                    pendingDeleteAction = i;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                DrawActionNameField(elem.FindPropertyRelative("actionName"));

                var effectsList = elem.FindPropertyRelative("effects");
                if (effectsList != null)
                    EditorGUILayout.PropertyField(effectsList, new GUIContent("触发效果"), true);

                EditorGUILayout.EndVertical();
            }

            var actionEnum = GetBuffActionEnumType();
            if (actionEnum != null)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var name in Enum.GetNames(actionEnum).Take(5))
                {
                    if (name == "None") continue;
                    if (GUILayout.Button($"+ {name}", GUILayout.Height(22)))
                        AddAction(list, name);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ 添加事件", GUILayout.Height(22)))
                AddAction(list, "NewAction");

            EditorGUILayout.EndVertical();
        }

        private void DrawActionNameField(SerializedProperty prop)
        {
            var enumType = GetBuffActionEnumType();
            if (enumType != null && Enum.TryParse(enumType, prop.stringValue, out var enumVal))
            {
                var newVal = EditorGUILayout.EnumPopup("TriggerEvent", (Enum)enumVal);
                if (newVal.ToString() != prop.stringValue)
                    prop.stringValue = newVal.ToString();
            }
            else
            {
                prop.stringValue = EditorGUILayout.TextField("TriggerEvent", prop.stringValue);
            }
        }

        private void AddAction(SerializedProperty list, string name)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("actionName").stringValue = name;
        }

        // ===== 效果执行器 =====
        private void DrawEffects()
        {
            DrawSectionHeader("效果执行器");
            var list = serializedObject.FindProperty("effectExecuters");

            if (list == null)
            {
                EditorGUILayout.HelpBox("未找到 effectExecuters 字段", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            int count = list.arraySize;
            if (count == 0)
                EditorGUILayout.HelpBox("无效果执行器，点击按钮添加", MessageType.Info);

            for (int i = 0; i < count; i++)
            {
                var elem = list.GetArrayElementAtIndex(i);
                if (elem == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"执行器 #{i + 1}", EditorStyles.boldLabel);
                GUI.backgroundColor = RemoveColor;
                if (GUILayout.Button("Remove", GUILayout.Width(55), GUILayout.Height(18)))
                    pendingDeleteExecuter = i;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                var modeProp = elem.FindPropertyRelative("executionMode");
                if (modeProp != null)
                    EditorGUILayout.PropertyField(modeProp, new GUIContent("执行模式"), true);

                var effectsProp = elem.FindPropertyRelative("effects");
                if (effectsProp != null)
                    EditorGUILayout.PropertyField(effectsProp, new GUIContent("效果列表"), true);

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 添加执行器", GUILayout.Height(24)))
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                var newElem = list.GetArrayElementAtIndex(list.arraySize - 1);
                newElem.managedReferenceValue = new BuffEffectExecuterBase();
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif