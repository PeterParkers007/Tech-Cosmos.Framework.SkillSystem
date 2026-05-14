using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public interface IBaseLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        public List<string> TriggerEvents { get; set; }
        public void Trigger(SkillContext<T> context);
    }
}
