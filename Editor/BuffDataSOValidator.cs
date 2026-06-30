#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// BuffDataSO 资产校验器，检查配置完整性与常见错误。
    /// </summary>
    public static class BuffDataSOValidator
    {
        /// <summary>校验单个 BuffDataSO 并返回问题列表。</summary>
        public static List<string> Validate(BuffDataSO buffDataSO)
        {
            var issues = new List<string>();
            if (buffDataSO == null)
            {
                issues.Add("BuffDataSO 为空");
                return issues;
            }

            if (string.IsNullOrWhiteSpace(buffDataSO.buffName))
                issues.Add("Buff 名称为空");

            if (buffDataSO.maxStacks < 1)
                issues.Add($"maxStacks 应 >= 1，当前为 {buffDataSO.maxStacks}");

            if (buffDataSO.modifiers != null)
            {
                for (int i = 0; i < buffDataSO.modifiers.Count; i++)
                {
                    var modifier = buffDataSO.modifiers[i];
                    if (modifier == null)
                    {
                        issues.Add($"modifiers [{i}] 为空引用");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(modifier.modifyType))
                        issues.Add($"modifiers [{i}] 未配置 modifyType");
                }
            }

            if (buffDataSO.actions != null)
            {
                for (int i = 0; i < buffDataSO.actions.Count; i++)
                {
                    var action = buffDataSO.actions[i];
                    if (action == null)
                    {
                        issues.Add($"actions [{i}] 为空引用");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(action.actionName))
                        issues.Add($"actions [{i}] 未配置 actionName");

                    if (action.effects != null)
                    {
                        for (int j = 0; j < action.effects.Count; j++)
                        {
                            if (action.effects[j] == null)
                                issues.Add($"actions [{i}].effects [{j}] 为空引用");
                        }
                    }
                }
            }

            if (buffDataSO.effectExecuters != null)
            {
                for (int i = 0; i < buffDataSO.effectExecuters.Count; i++)
                {
                    if (buffDataSO.effectExecuters[i] == null)
                        issues.Add($"effectExecuters [{i}] 为空引用");
                }
            }

            return issues;
        }

        /// <summary>校验项目中所有 BuffDataSO 资产，返回问题总数。</summary>
        public static int ValidateAllAssets()
        {
            var guids = AssetDatabase.FindAssets("t:BuffDataSO");
            int issueCount = 0;
            var report = new StringBuilder();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<BuffDataSO>(path);
                var issues = Validate(so);
                if (issues.Count == 0) continue;

                issueCount += issues.Count;
                report.AppendLine($"[{path}]");
                foreach (var issue in issues)
                    report.AppendLine($"  - {issue}");
            }

            if (issueCount == 0)
                Debug.Log("[BuffDataSOValidator] 全部 Buff 资产校验通过");
            else
                Debug.LogWarning($"[BuffDataSOValidator] 发现 {issueCount} 个问题:\n{report}");

            return issueCount;
        }
    }
}
#endif
