#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using TechCosmos.SkillSystem.Runtime;

namespace TechCosmos.SkillSystem.Editor
{
    /// <summary>
    /// SkillDataSO 资产校验器，检查配置完整性与常见错误。
    /// </summary>
    public static class SkillDataSOValidator
    {
        /// <summary>校验单个 SkillDataSO 并返回问题列表。</summary>
        public static List<string> Validate(SkillDataSO skillDataSO)
        {
            var issues = new List<string>();
            if (skillDataSO == null)
            {
                issues.Add("SkillDataSO 为空");
                return issues;
            }

            if (string.IsNullOrWhiteSpace(skillDataSO.SkillName))
                issues.Add("技能名称为空");

            if (skillDataSO.TriggerEvents == null || skillDataSO.TriggerEvents.Count == 0)
                issues.Add("未配置触发事件");

            if (skillDataSO.Mechanisms == null || skillDataSO.Mechanisms.Count == 0)
                issues.Add("机制列表为空");

            if (skillDataSO.Timeline != null && skillDataSO.Timeline.enabled && skillDataSO.Timeline.totalDuration <= 0f)
                issues.Add("Timeline 已启用但总时长 <= 0");

            if (skillDataSO.useConditionTree && skillDataSO.conditionTreeRoot == null &&
                (skillDataSO.Conditions == null || skillDataSO.Conditions.Count == 0))
                issues.Add("未配置条件树或条件列表");

            if (skillDataSO.Conditions != null)
            {
                for (int i = 0; i < skillDataSO.Conditions.Count; i++)
                {
                    if (skillDataSO.Conditions[i] == null)
                        issues.Add($"条件 [{i}] 为空引用");
                }
            }

            if (skillDataSO.Mechanisms != null)
            {
                for (int i = 0; i < skillDataSO.Mechanisms.Count; i++)
                {
                    if (skillDataSO.Mechanisms[i] == null)
                        issues.Add($"机制 [{i}] 为空引用");
                }
            }

            if (skillDataSO.Profile != null && skillDataSO.Profile.channelTime > 0f && skillDataSO.Profile.castTime <= 0f)
                issues.Add("配置了引导时间但未配置施法时间");

            return issues;
        }

        /// <summary>校验项目中所有 SkillDataSO 资产，返回问题总数。</summary>
        public static int ValidateAllAssets()
        {
            var guids = AssetDatabase.FindAssets("t:SkillDataSO");
            int issueCount = 0;
            var report = new StringBuilder();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);
                var issues = Validate(so);
                if (issues.Count == 0) continue;

                issueCount += issues.Count;
                report.AppendLine($"[{path}]");
                foreach (var issue in issues)
                    report.AppendLine($"  - {issue}");
            }

            if (issueCount == 0)
                Debug.Log("[SkillDataSOValidator] 全部技能资产校验通过");
            else
                Debug.LogWarning($"[SkillDataSOValidator] 发现 {issueCount} 个问题:\n{report}");

            return issueCount;
        }
    }
}
#endif
