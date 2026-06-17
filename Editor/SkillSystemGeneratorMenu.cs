#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// 技能系统代码生成器的 Unity 菜单入口，提供生成、清理与校验命令。
    /// </summary>
    public static class SkillSystemGeneratorMenu
    {
        // Generate ALL Classes 已在 MechanismCodeGeneratorV2 中定义

        /// <summary>仅生成 SkillDataSO 类，不生成机制与条件。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Generate SkillDataSO Only", priority = 12)]
        public static void GenerateSkillDataSOs()
        {
            SkillDataSOGenerator.GenerateAllSkillDataSO();
        }

        /// <summary>删除 Assets/Generated 下所有自动生成的 .g.cs 文件。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Clear All Generated", priority = 100)]
        public static void ClearAllGenerated()
        {
            if (EditorUtility.DisplayDialog("确认清理",
                "这将删除所有生成的代码文件。\n\n确定要继续吗？",
                "确定", "取消"))
            {
                string[] folders = {
                    "Assets/Generated/Mechanisms",
                    "Assets/Generated/Conditions",
                    "Assets/Generated/SkillDataSO"
                };

                int deleted = 0;
                foreach (var folder in folders)
                {
                    if (System.IO.Directory.Exists(folder))
                    {
                        foreach (var file in System.IO.Directory.GetFiles(folder, "*.g.cs"))
                        {
                            System.IO.File.Delete(file);
                            deleted++;
                        }
                        System.IO.Directory.Delete(folder);
                    }
                }

                if (System.IO.Directory.Exists("Assets/Generated"))
                {
                    if (System.IO.Directory.GetFileSystemEntries("Assets/Generated").Length == 0)
                        System.IO.Directory.Delete("Assets/Generated");
                }

                AssetDatabase.Refresh();
                Debug.Log($"🗑 已清理 {deleted} 个生成文件");
            }
        }

        /// <summary>更新 TriggerEventType 枚举文件。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Update TriggerEvent Enum", priority = 13)]
        public static void UpdateTriggerEventEnum()
        {
            TriggerEventEnumGenerator.UpdateTriggerEventEnum();
        }

        /// <summary>校验项目中所有 SkillDataSO 资产并输出问题报告。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Generator/Validate All Skill Assets", priority = 14)]
        public static void ValidateAllSkillAssets()
        {
            SkillDataSOValidator.ValidateAllAssets();
        }

        /// <summary>显示技能系统代码生成器的使用说明对话框。</summary>
        [MenuItem("Tech-Cosmos/SkillSystem/Documentation", priority = 1000)]
        public static void OpenDocumentation()
        {
            EditorUtility.DisplayDialog("技能系统代码生成器",
                "📋 使用步骤：\n\n" +
                "1. 在 IUnit<T> 实现类上添加 [GenerateSkillDataSO]\n" +
                "2. 在字段上添加 [SkillDataField] 或 [SkillDataEntry]\n" +
                "3. 在 Mechanism<T> 子类上添加 [AutoGenerateMechanism]\n" +
                "4. 在 Condition<T> 子类上添加 [AutoGenerateCondition]\n" +
                "5. 运行 Generate ALL Classes\n\n" +
                "📁 生成文件夹：\n" +
                "• Assets/Generated/Mechanisms/\n" +
                "• Assets/Generated/Conditions/\n" +
                "• Assets/Generated/SkillDataSO/\n\n" +
                "💡 提示：生成的 .g.cs 文件可以加入 .gitignore",
                "知道了");
        }
    }
}
#endif