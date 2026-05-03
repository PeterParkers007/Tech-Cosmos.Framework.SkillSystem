#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Editor
{
    public class TriggerEventEnumEditor : EditorWindow
    {
        private List<string> events = new();
        private string newEventName = "";
        private Vector2 scrollPos;
        private bool dirty;

        private const string ENUM_FILE_PATH = "Assets/Generated/TriggerEventType.cs";

        [MenuItem("Tech-Cosmos/SkillSystem/TriggerEvent Enum Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<TriggerEventEnumEditor>("TriggerEvent 枚举编辑器");
            window.minSize = new Vector2(350, 400);
            window.Show();
        }

        void OnEnable()
        {
            LoadFromEnumFile();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TriggerEvent 枚举编辑器", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "在这里管理 TriggerEventType 枚举的所有值。\n" +
                "点击「生成枚举文件」会覆盖写入枚举代码。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 添加新事件
            EditorGUILayout.LabelField("添加新事件", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newEventName = EditorGUILayout.TextField(newEventName);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("添加", GUILayout.Width(60)) && !string.IsNullOrWhiteSpace(newEventName))
            {
                var name = SanitizeEventName(newEventName);
                if (!events.Contains(name))
                {
                    events.Add(name);
                    newEventName = "";
                    dirty = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", $"事件 '{name}' 已存在", "确定");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 已有事件列表
            EditorGUILayout.LabelField($"已有事件 ({events.Count})", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));

            for (int i = 0; i < events.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));
                string newName = EditorGUILayout.TextField(events[i]);
                if (newName != events[i])
                {
                    events[i] = SanitizeEventName(newName);
                    dirty = true;
                }

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    events.RemoveAt(i);
                    dirty = true;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("生成枚举文件", GUILayout.Height(35)))
            {
                GenerateEnumFile();
                dirty = false;
                EditorUtility.DisplayDialog("完成", $"枚举文件已生成，共 {events.Count} 个事件。", "确定");
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("从文件重新加载", GUILayout.Height(35)))
            {
                LoadFromEnumFile();
                dirty = false;
            }

            EditorGUILayout.EndHorizontal();

            if (dirty)
            {
                EditorGUILayout.HelpBox("有未保存的修改", MessageType.Warning);
            }
        }

        private void OnDisable()
        {
            if (dirty)
            {
                if (EditorUtility.DisplayDialog("未保存", "有修改未保存，要现在生成吗？", "生成", "放弃"))
                {
                    GenerateEnumFile();
                }
            }
        }

        private void LoadFromEnumFile()
        {
            events.Clear();
            if (!File.Exists(ENUM_FILE_PATH))
            {
                events.Add("OnAttack");
                events.Add("OnDamaged");
                events.Add("OnHeal");
                return;
            }

            var lines = File.ReadAllLines(ENUM_FILE_PATH);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("//")) continue;
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed == "{" || trimmed == "}" || trimmed.Contains("namespace") || trimmed.Contains("public enum")) continue;

                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"^\s*(\w+)\s*[=,;]");
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    if (name != "None")
                        events.Add(name);
                }
            }
        }

        private void GenerateEnumFile()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            sb.AppendLine("// 由 TriggerEvent 枚举编辑器生成");
            sb.AppendLine();
            sb.AppendLine("namespace TechCosmos.SkillSystem.Runtime");
            sb.AppendLine("{");
            sb.AppendLine("    public enum TriggerEventType");
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");
            sb.AppendLine();

            for (int i = 0; i < events.Count; i++)
                sb.AppendLine($"        {events[i]} = {i + 1},");

            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public static class TriggerEventTypeExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        public static string ToEventString(this TriggerEventType type) => type.ToString();");
            sb.AppendLine();
            sb.AppendLine("        public static TriggerEventType FromString(string eventString)");
            sb.AppendLine("        {");
            sb.AppendLine("            System.Enum.TryParse(eventString, out TriggerEventType result);");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var dir = Path.GetDirectoryName(ENUM_FILE_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(ENUM_FILE_PATH, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"✅ TriggerEventType 已生成到 {ENUM_FILE_PATH}，共 {events.Count} 个事件");
        }

        private string SanitizeEventName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "UnnamedEvent";
            name = name.Trim().Replace(" ", "_");
            if (!char.IsLetter(name[0]) && name[0] != '_')
                name = "_" + name;
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[^\w]", "");
            return name;
        }
    }
}
#endif