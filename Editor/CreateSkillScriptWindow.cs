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
    /// <summary>
    /// 创建机制或条件脚本模板的编辑器窗口。
    /// </summary>
    public class CreateSkillScriptWindow : EditorWindow
    {
        private enum ScriptType { Mechanism, Condition }

        private ScriptType scriptType = ScriptType.Mechanism;
        private string className = "NewMechanism";
        private string namespaceName = "TechCosmos.SkillSystem.Runtime";
        private Type selectedUnitType;
        private List<Type> unitTypes = new();
        private Vector2 scrollPos;
        private string searchFilter = "";
        private bool typesDirty = true;
        /// <summary>打开创建技能脚本窗口。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Create Skill Script", priority = 30)]
        public static void OpenWindow()
        {
            var window = GetWindow<CreateSkillScriptWindow>("创建技能脚本");
            window.minSize = new Vector2(450, 350);
            window.maxSize = new Vector2(450, 600);
            window.Show();
        }

        /// <summary>从 Assets 菜单快速创建机制脚本模板。</summary>
        [MenuItem("Assets/Create/Tech-Cosmos/Skill Mechanism", priority = 30)]
        public static void CreateMechanismFromMenu()
        {
            ShowWindowAndCreate(ScriptType.Mechanism);
        }

        /// <summary>从 Assets 菜单快速创建条件脚本模板。</summary>
        [MenuItem("Assets/Create/Tech-Cosmos/Skill Condition", priority = 31)]
        public static void CreateConditionFromMenu()
        {
            ShowWindowAndCreate(ScriptType.Condition);
        }

        private static void ShowWindowAndCreate(ScriptType type)
        {
            var window = GetWindow<CreateSkillScriptWindow>("创建技能脚本");
            window.scriptType = type;
            window.className = type == ScriptType.Mechanism ? "NewMechanism" : "NewCondition";
            window.selectedUnitType = null;
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

            // 标题
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("创建技能脚本", titleStyle);
            EditorGUILayout.Space(5);

            // 脚本类型
            EditorGUILayout.LabelField("脚本类型", EditorStyles.boldLabel);
            scriptType = (ScriptType)EditorGUILayout.EnumPopup(scriptType);
            EditorGUILayout.Space(10);

            // 类名
            EditorGUILayout.LabelField("类名", EditorStyles.boldLabel);
            className = EditorGUILayout.TextField(className);
            if (string.IsNullOrWhiteSpace(className))
                EditorGUILayout.HelpBox("类名不能为空", MessageType.Error);
            else if (!char.IsLetter(className[0]))
                EditorGUILayout.HelpBox("类名必须以字母开头", MessageType.Error);
            EditorGUILayout.Space(10);

            // 命名空间
            EditorGUILayout.LabelField("命名空间", EditorStyles.boldLabel);
            namespaceName = EditorGUILayout.TextField(namespaceName);
            EditorGUILayout.Space(10);

            // Unit 类型选择
            EditorGUILayout.LabelField("目标 Unit 类型", EditorStyles.boldLabel);

            // 搜索
            var searchStyle = new GUIStyle("SearchTextField");
            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField(searchFilter, searchStyle);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
                searchFilter = "";
            EditorGUILayout.EndHorizontal();

            RefreshUnitTypes();

            // 过滤
            var filteredTypes = string.IsNullOrEmpty(searchFilter)
                ? unitTypes
                : unitTypes.Where(t => t.Name.ToLower().Contains(searchFilter.ToLower())).ToList();

            if (filteredTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到实现 IUnit<> 的类型", MessageType.Warning);
            }
            else
            {
                // 可滚动的类型列表
                var listHeight = Mathf.Min(filteredTypes.Count * 22, 150);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(listHeight + 10));

                foreach (var unitType in filteredTypes)
                {
                    bool isSelected = selectedUnitType == unitType;

                    // 高亮选中
                    if (isSelected)
                    {
                        var rect = EditorGUILayout.BeginHorizontal();
                        EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.3f));
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                    }

                    string displayName = unitType.Name;
                    string ns = unitType.Namespace;
                    if (!string.IsNullOrEmpty(ns) && ns != "TechCosmos.SkillSystem.Runtime")
                        displayName += $"  ({ns})";

                    if (GUILayout.Toggle(isSelected, displayName, EditorStyles.label, GUILayout.ExpandWidth(true)))
                    {
                        selectedUnitType = unitType;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // 默认 Unit 类型提示
            if (selectedUnitType == null && unitTypes.Count > 0)
            {
                var tipStyle = new GUIStyle(EditorStyles.miniLabel);
                tipStyle.normal.textColor = new Color(0.8f, 0.6f, 0.2f);
                EditorGUILayout.LabelField($"未选择时将使用第一个类型: {unitTypes[0].Name}", tipStyle);
            }

            EditorGUILayout.Space(15);

            // 创建按钮
            bool canCreate = !string.IsNullOrWhiteSpace(className) && char.IsLetter(className[0]);

            EditorGUI.BeginDisabledGroup(!canCreate);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("创建脚本", GUILayout.Height(35)))
            {
                // 没选就用第一个
                if (selectedUnitType == null && unitTypes.Count > 0)
                    selectedUnitType = unitTypes[0];

                CreateScript(scriptType);
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
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUnit<>)))
                .OrderBy(t => t.Name)
                .ToList();

            typesDirty = false;
        }

        private void CreateScript(ScriptType type)
        {
            string folderPath = GetCurrentProjectPath();

            // 用用户输入的类名作为文件名
            string fileName = className + ".cs";
            string filePath = Path.Combine(folderPath, fileName);
            filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);

            // 重新提取类名（因为有唯一化处理可能加了数字）
            string finalClassName = Path.GetFileNameWithoutExtension(filePath);
            string namespaceName = !string.IsNullOrWhiteSpace(this.namespaceName)
                ? this.namespaceName
                : GetNamespaceFromPath(filePath);
            string unitTypeName = selectedUnitType?.Name ?? unitTypes.FirstOrDefault()?.Name ?? "Character";

            // 读取模板
            string templatePath = FindTemplatePath(type == ScriptType.Mechanism
                ? "MechanismTemplate.cs.txt" : "ConditionTemplate.cs.txt");

            string code;
            if (!string.IsNullOrEmpty(templatePath))
            {
                code = File.ReadAllText(templatePath)
                    .Replace("#NAMESPACE#", namespaceName)
                    .Replace("#CLASSNAME#", finalClassName)
                    .Replace("#UNITTYPE#", unitTypeName);
            }
            else
            {
                code = type == ScriptType.Mechanism
                    ? GenerateMechanismCode(finalClassName, namespaceName, unitTypeName)
                    : GenerateConditionCode(finalClassName, namespaceName, unitTypeName);
            }

            File.WriteAllText(filePath, code);
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private string GenerateMechanismCode(string className, string ns, string unitType)
        {
            return $@"using System;
using TechCosmos.SkillSystem.Runtime;
using UnityEngine;

namespace {ns}
{{
    [Serializable]
    [AutoGenerateMechanism(typeof({unitType}))]
    public class {className}<T> : Mechanism<T> where T : class, IUnit<T>
    {{
        // [SerializeField] private float exampleValue = 1f;

        public override void Execute(SkillContext<T> context, IDataLayer<T> dataLayer)
        {{
            
        }}
    }}
}}";
        }

        private string GenerateConditionCode(string className, string ns, string unitType)
        {
            return $@"using System;
using TechCosmos.SkillSystem.Runtime;
using UnityEngine;

namespace {ns}
{{
    [Serializable]
    [AutoGenerateCondition(typeof({unitType}))]
    public class {className}<T> : Condition<T> where T : class, IUnit<T>
    {{
        // [SerializeField] private float exampleValue = 1f;

        public override bool IsEligible(SkillContext<T> skillContext, IDataLayer<T> dataLayer)
        {{
            return true;
        }}
    }}
}}";
        }

        private static string FindTemplatePath(string templateName)
        {
            var guids = AssetDatabase.FindAssets(templateName.Replace(".txt", ""));
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(templateName))
                    return path;
            }
            return null;
        }

        private static string GetCurrentProjectPath()
        {
            var selected = Selection.activeObject;
            if (selected != null)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(path))
                {
                    return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path);
                }
            }
            return "Assets";
        }

        private static string GetNamespaceFromPath(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath).Replace("\\", "/");
            if (dir.StartsWith("Assets/Scripts/"))
                return dir.Substring("Assets/Scripts/".Length).Replace("/", ".");
            if (dir.StartsWith("Assets/"))
                return dir.Substring("Assets/".Length).Replace("/", ".");
            return "TechCosmos.SkillSystem.Runtime";
        }
    }
}
#endif