using System;
using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能运行配置，描述优先级、施法时长及可打断性等属性。
    /// </summary>
    [Serializable]
    public class SkillProfile
    {
        /// <summary>事件触发与执行的优先级，数值越大越先执行。</summary>
        public int executionPriority;
        /// <summary>前摇/施法时间（秒）。</summary>
        public float castTime;
        /// <summary>引导/持续施法时间（秒）。</summary>
        public float channelTime;
        /// <summary>是否可被外部打断。</summary>
        public bool canBeInterrupted = true;
        /// <summary>技能标签列表，用于分类与条件判断。</summary>
        public List<string> tags = new();
    }
}
