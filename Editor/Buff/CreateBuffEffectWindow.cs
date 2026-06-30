#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    public class CreateBuffEffectWindow : EditorWindow
    {
        private enum ScriptType { BuffEffect, ExecutionMode }

        private ScriptType scriptType = ScriptType.BuffEffect;
        private string className = "NewBuffEffect";
        private string namespaceName = "TechCosmos.SkillSystem.Runtime.Effects";
        private string menuCategory = "Custom";
        private string displayName = "";
        private Type selectedUnitType;
        private List<Type> unitTypes = new();
        private Vector2 scrollPos;
        private string searchFilter = "";
        private bool typesDirty = true;

        [MenuItem("Tech-Cosmos/SkillSystem/Create Buff Script", priority = 31)]
        public static void OpenWindow()
        {
            var window = GetWindow<CreateBuffEffectWindow>("创建 Buff 脚本");
            window.minSize = new Vector2(450, 420);
            window.Show();
        }

        [MenuItem("Assets/Create/SkillSystem/Buff Effect", priority = 40)]
        public static void CreateBuffEffectFromMenu()
        {
            var window = GetWindow<CreateBuffEffectWindow>("创建 Buff 脚本");
            window.scriptType = ScriptType.BuffEffect;
            window.className = "NewBuffEffect";
            window.ShowModal();
        }

        [MenuItem("Assets/Create/SkillSystem/Execution Mode", priority = 41)]
        public static void CreateExecutionModeFromMenu()
        {
            var window = GetWindow<CreateBuffEffectWindow>("创建 Buff 脚本");
            window.scriptType = ScriptType.ExecutionMode;
            window.className = "NewExecutionMode";
            window.ShowModal();
        }

        void OnEnable()
        {
            typesDirty = true;
            RefreshUnitTypes();
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(10);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("创建 Buff 脚本", titleStyle);
            EditorGUILayout.Space(10);

            // 脚本类型
            EditorGUILayout.LabelField("脚本类型", EditorStyles.boldLabel);
            scriptType = (ScriptType)EditorGUILayout.EnumPopup(scriptType);
            EditorGUILayout.Space(10);

            // 类名
            EditorGUILayout.LabelField("类名", EditorStyles.boldLabel);
            className = EditorGUILayout.TextField(className);
            if (string.IsNullOrWhiteSpace(className))
                EditorGUILayout.HelpBox("类名不能为空", MessageType.Error);
            else if (!char.IsLetter(className[0]) && className[0] != '_')
                EditorGUILayout.HelpBox("类名必须以字母或下划线开头", MessageType.Error);
            EditorGUILayout.Space(10);

            // 命名空间
            EditorGUILayout.LabelField("命名空间", EditorStyles.boldLabel);
            namespaceName = EditorGUILayout.TextField(namespaceName);
            EditorGUILayout.Space(10);

            // 目标单位类型选择
            EditorGUILayout.LabelField("目标单位类型", EditorStyles.boldLabel);

            // 搜索
            var searchStyle = new GUIStyle("SearchTextField");
            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField(searchFilter, searchStyle);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
                searchFilter = "";
            EditorGUILayout.EndHorizontal();

            RefreshUnitTypes();

            var filteredTypes = string.IsNullOrEmpty(searchFilter)
                ? unitTypes
                : unitTypes.Where(t => t.Name.ToLower().Contains(searchFilter.ToLower())).ToList();

            if (filteredTypes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "没有找到标记了 [ApplyBuffTarget] 的类型\n" +
                    "请在目标单位类上添加 [ApplyBuffTarget] 特性",
                    MessageType.Warning);
            }
            else
            {
                var listHeight = Mathf.Min(filteredTypes.Count * 22, 150);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(listHeight + 10));

                for (int i = 0; i < filteredTypes.Count; i++)
                {
                    var unitType = filteredTypes[i];
                    bool isSelected = selectedUnitType == unitType;

                    EditorGUILayout.BeginHorizontal();

                    if (isSelected)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.3f));
                        EditorGUILayout.Space(rect.x);
                    }

                    string displayText = unitType.Name;
                    if (!string.IsNullOrEmpty(unitType.Namespace))
                        displayText += $"  ({unitType.Namespace})";

                    if (GUILayout.Toggle(isSelected, displayText, EditorStyles.label, GUILayout.ExpandWidth(true)))
                    {
                        selectedUnitType = unitType;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            // 默认类型提示
            if (selectedUnitType == null && unitTypes.Count > 0)
            {
                var tipStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.8f, 0.6f, 0.2f) }
                };
                EditorGUILayout.LabelField($"未选择时默认使用: {unitTypes[0].Name}", tipStyle);
            }

            EditorGUILayout.Space(5);

            // BuffEffect 额外配置
            if (scriptType == ScriptType.BuffEffect)
            {
                EditorGUILayout.LabelField("菜单分类", EditorStyles.boldLabel);
                menuCategory = EditorGUILayout.TextField(menuCategory);

                EditorGUILayout.LabelField("显示名称（留空使用类名）", EditorStyles.boldLabel);
                displayName = EditorGUILayout.TextField(displayName);
            }

            EditorGUILayout.Space(15);

            // 创建按钮
            bool canCreate = !string.IsNullOrWhiteSpace(className) &&
                             (char.IsLetter(className[0]) || className[0] == '_');

            EditorGUI.BeginDisabledGroup(!canCreate);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("创建脚本", GUILayout.Height(35)))
            {
                if (selectedUnitType == null && unitTypes.Count > 0)
                    selectedUnitType = unitTypes[0];

                CreateScript();
                Close();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }

        private void RefreshUnitTypes()
        {
            if (!typesDirty && unitTypes.Count > 0) return;

            unitTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try { return a.GetExportedTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetCustomAttributes(typeof(ApplyBuffTargetAttribute), false).Any())
                .OrderBy(t => t.Name)
                .ToList();

            typesDirty = false;
        }

        private void CreateScript()
        {
            string folderPath = GetCurrentProjectPath();
            string fileName = className + ".cs";
            string filePath = Path.Combine(folderPath, fileName);
            filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);

            string finalClassName = Path.GetFileNameWithoutExtension(filePath);
            string unitTypeName = selectedUnitType?.Name ?? unitTypes.FirstOrDefault()?.Name ?? "object";

            string code = scriptType == ScriptType.BuffEffect
                ? GenerateBuffEffectCode(finalClassName, unitTypeName)
                : GenerateExecutionModeCode(finalClassName, unitTypeName);

            File.WriteAllText(filePath, code);
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private string GenerateBuffEffectCode(string finalClassName, string unitTypeName)
        {
            string autoGenAttr = $"[AutoGenerateBuffEffect(typeof({unitTypeName}))]";
            string menuAttr = $"[BuffEffectMenu(\"{menuCategory}\"";

            if (!string.IsNullOrEmpty(displayName))
                menuAttr += $", DisplayName = \"{displayName}\"";
            menuAttr += ")]";

            return $@"using System;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace {namespaceName}
{{
    /// <summary>
    /// {finalClassName} —— 对 {unitTypeName} 的 Buff 效果
    /// </summary>
    [Serializable]
    {autoGenAttr}
    {menuAttr}
    public class {finalClassName}<T> : BuffEffect<T> where T : class
    {{
        [SerializeField] private float value = 1f;

        public override void Execute(T target, BuffContext<T> context)
        {{
            // 在这里实现效果逻辑
        }}
    }}
}}
";
        }

        private string GenerateExecutionModeCode(string finalClassName, string unitTypeName)
        {
            string autoGenAttr = $"[AutoGenerateBuffEffect(typeof({unitTypeName}))]";

            return $@"using System;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace {namespaceName}
{{
    /// <summary>
    /// {finalClassName} —— 对 {unitTypeName} 的自定义执行模式
    /// </summary>
    [Serializable]
    {autoGenAttr}
    public class {finalClassName}<T> : ExecutionMode<T> where T : class
    {{
        [SerializeField] private float _interval = 1f;
        [SerializeField] private float _nextTime;

        public override bool IsEligible()
        {{
            return Time.time >= _nextTime;
        }}

        public override void MarkExecuted()
        {{
            _nextTime = Time.time + _interval;
        }}
    }}
}}
";
        }

        private static string GetCurrentProjectPath()
        {
            var selected = Selection.activeObject;
            if (selected != null)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(path))
                {
                    return AssetDatabase.IsValidFolder(path)
                        ? path
                        : Path.GetDirectoryName(path);
                }
            }
            return "Assets";
        }
    }
}
#endif