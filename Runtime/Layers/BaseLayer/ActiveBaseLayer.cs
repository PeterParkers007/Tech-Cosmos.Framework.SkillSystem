using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public class ActiveBaseLayer<T> : BaseLayer<T> where T : class, IUnit<T>
    {
        public ActiveBaseLayer(List<string> triggerEvents) : base(triggerEvents) { }

        public override void Trigger(SkillContext<T> skillContext)
            => Skill.ExecuteLayer.Execute(skillContext);
    }
}
