using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public class PassiveBaseLayer<T> : BaseLayer<T> where T : class, IUnit<T>
    {
        public PassiveBaseLayer(List<string> triggerEvents) : base(triggerEvents) { }

        public override void Trigger(SkillContext<T> context)
            => Skill.ExecuteLayer.Execute(context);
    }
}
