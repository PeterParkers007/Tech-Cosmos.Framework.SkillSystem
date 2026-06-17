using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 被动技能基础层：装备时自动生效，同时仍可响应触发事件。
    /// </summary>
    public class PassiveBaseLayer<T> : BaseLayer<T> where T : class, IUnit<T>
    {
        public PassiveBaseLayer(List<string> triggerEvents) : base(triggerEvents) { }

        public override void Trigger(SkillContext<T> context)
            => Skill.ExecuteLayer.Execute(context);
    }
}
