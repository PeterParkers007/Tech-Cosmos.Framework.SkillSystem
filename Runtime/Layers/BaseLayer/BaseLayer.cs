using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public abstract class BaseLayer<T> : IBaseLayer<T> where T : class, IUnit<T>
    {
        public ISkill<T> Skill { get; set; }
        public List<string> TriggerEvents { get; set; }

        public BaseLayer(List<string> triggerEvents)
        {
            this.TriggerEvents = triggerEvents ?? new List<string>();
        }

        public virtual void Trigger(SkillContext<T> context) { }
    }
}
