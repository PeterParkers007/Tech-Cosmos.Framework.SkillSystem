using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>时间轴片段类型。</summary>
    public enum SkillTimelineClipType
    {
        /// <summary>执行机制。</summary>
        Mechanism,
        /// <summary>触发单位事件。</summary>
        EventMarker,
        /// <summary>阶段标记（仅用于编辑器展示）。</summary>
        PhaseLabel
    }

    /// <summary>技能时间轴上的一个片段。</summary>
    [Serializable]
    public class SkillTimelineClip
    {
        /// <summary>片段显示名称。</summary>
        public string label = "Clip";
        /// <summary>片段类型。</summary>
        public SkillTimelineClipType clipType = SkillTimelineClipType.Mechanism;
        /// <summary>开始时间（秒）。</summary>
        public float startTime;
        /// <summary>片段时长（秒）。</summary>
        public float duration = 0.1f;
        /// <summary>事件标记名（EventMarker 类型使用）。</summary>
        public string eventName;
        [UnityEngine.SerializeReference]
        /// <summary>要执行的机制（Mechanism 类型使用）。</summary>
        public MechanismBase mechanism;
    }

    /// <summary>
    /// 技能时间轴数据：定义技能执行后的分段时间效果。
    /// </summary>
    [Serializable]
    public class SkillTimelineData
    {
        /// <summary>是否启用时间轴。</summary>
        public bool enabled;
        /// <summary>时间轴总时长（秒）。</summary>
        public float totalDuration = 1f;
        /// <summary>时间轴片段列表。</summary>
        public List<SkillTimelineClip> clips = new();
    }
}
