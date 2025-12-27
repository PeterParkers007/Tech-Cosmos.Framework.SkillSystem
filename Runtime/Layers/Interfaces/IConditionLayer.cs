using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public interface IConditionLayer<T> : ISkillLayer<T> where T : class, IUnit<T>
    {
        public List<Condition<T>> Conditions { get; set; }
        public bool CheckCondition(SkillContext<T> skillContext);
    }
}
