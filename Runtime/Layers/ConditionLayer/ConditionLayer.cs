using System.Collections.Generic;
namespace TechCosmos.SkillSystem.Runtime
{
    public class ConditionLayer<T> : IConditionLayer<T> where T : class, IUnit<T>
    {
        public List<Condition<T>> Conditions { get; set; }
        public ISkill<T> Skill { get; set; } 

        public bool CheckCondition(SkillContext<T> skillContext)
        {
            var conditions = Conditions; 
            int count = conditions.Count;

            for (int i = 0; i < count; i++)
            {
                if (!conditions[i].IsEligible(skillContext,Skill.DataLayer))
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