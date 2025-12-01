using System.Collections.Generic;
using System.Linq;
namespace TechCosmos.SkillSystem.Runtime
{
    public class ConditionLayer<T> : IConditionLayer<T> where T : class, IUnit<T>
    {
        public List<Condition<T>> Conditions { get; set; }
        public ISkill<T> Skill { get; set; }

        public bool CheckCondition(SkillContext<T> skillContext)
        {
            foreach (var condition in Conditions)
            {
                if (!condition.IsEligible(skillContext))
                    return false;
            }
            return true;
        }

        public ConditionLayer(List<Condition<T>> conditions = null)
        {
            this.Conditions = conditions ?? new List<Condition<T>>();
        }
    }
}
