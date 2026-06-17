using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 基础层接口：定义触发事件与触发入口。
    /// </summary>
    public interface IBaseLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        /// <summary>可触发本技能的事件名列表。</summary>
        public List<string> TriggerEvents { get; set; }
        /// <summary>响应单位事件并尝试执行技能。</summary>
        public void Trigger(SkillContext<T> context);
    }
}
