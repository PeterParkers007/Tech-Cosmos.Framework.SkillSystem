#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// 技能系统 Unity 菜单入口（精简版）。
    /// </summary>
    public static class SkillSystemGeneratorMenu
    {
        /// <summary>删除 Assets/Generated 下所有自动生成的代码。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Clear Generated Code", priority = 21)]
        public static void ClearAllGenerated()
        {
            if (!EditorUtility.DisplayDialog("确认清理",
                "这将删除所有生成的代码文件。\n\n确定要继续吗？",
                "确定", "取消"))
            {
                return;
            }

            string[] folders =
            {
                "Assets/Generated/Mechanisms",
                "Assets/Generated/Conditions",
                "Assets/Generated/SkillDataSO",
                "Assets/Generated/SkillSystem/Effects",
                "Assets/Generated/SkillSystem/ExecutionModes"
            };

            int deleted = 0;
            string[] enumFiles =
            {
                SkillSystemEnumCatalog.TriggerEventPath,
                SkillSystemEnumCatalog.BuffModifyTypePath,
                SkillSystemEnumCatalog.BuffTagPath,
                "Assets/Generated/SkillSystem/BuffModifyType.cs",
                "Assets/Generated/SkillSystem/BuffActionType.cs",
                "Assets/Generated/SkillSystem/BuffTag.cs"
            };

            foreach (var enumFile in enumFiles)
            {
                if (!System.IO.File.Exists(enumFile)) continue;
                System.IO.File.Delete(enumFile);
                deleted++;
            }

            foreach (var folder in folders)
            {
                if (!System.IO.Directory.Exists(folder)) continue;

                foreach (var file in System.IO.Directory.GetFiles(folder, "*.g.cs"))
                {
                    System.IO.File.Delete(file);
                    deleted++;
                }

                System.IO.Directory.Delete(folder);
            }

            if (System.IO.Directory.Exists("Assets/Generated/SkillSystem") &&
                System.IO.Directory.GetFileSystemEntries("Assets/Generated/SkillSystem").Length == 0)
            {
                System.IO.Directory.Delete("Assets/Generated/SkillSystem");
            }

            if (System.IO.Directory.Exists("Assets/Generated") &&
                System.IO.Directory.GetFileSystemEntries("Assets/Generated").Length == 0)
            {
                System.IO.Directory.Delete("Assets/Generated");
            }

            AssetDatabase.Refresh();
            Debug.Log($"🗑 已清理 {deleted} 个生成文件");
        }

        /// <summary>根据 Enum Editor 配置与项目资产重新生成全部枚举文件。</summary>
        public static void RegenerateAllEnums()
        {
            TriggerEventEnumGenerator.UpdateTriggerEventEnum();

            foreach (var kind in SkillSystemEnumCatalog.AllKinds)
            {
                if (kind == SkillSystemEnumKind.TriggerEvent)
                    continue;
                SkillSystemEnumGenerator.WriteEnum(kind, SkillSystemEnumGenerator.LoadEnumValues(kind));
            }
        }

        /// <summary>校验项目中所有 Skill / Buff 资产并输出问题报告。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Validate Assets", priority = 20)]
        public static void ValidateAllAssets()
        {
            int skillIssues = SkillDataSOValidator.ValidateAllAssets();
            int buffIssues = BuffDataSOValidator.ValidateAllAssets();
            Debug.Log($"[SkillSystem] 校验完成：Skill {skillIssues} 项问题，Buff {buffIssues} 项问题");
        }

        /// <summary>显示技能系统使用说明。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Help", priority = 1000)]
        public static void OpenDocumentation()
        {
            EditorUtility.DisplayDialog("技能系统",
                "📋 常用流程：\n\n" +
                "1. 在 IUnit<T> 上添加 [GenerateSkillDataSO]\n" +
                "2. 在 Mechanism<T> / Condition<T> 上添加代码生成特性\n" +
                "3. 运行 Generate All\n" +
                "4. 用 Skill / Buff / Graph / Enum Editor 编辑资产\n\n" +
                "📁 生成目录：Assets/Generated/\n\n" +
                "💡 生成的 .g.cs 可加入 .gitignore",
                "知道了");
        }
    }
}
#endif
