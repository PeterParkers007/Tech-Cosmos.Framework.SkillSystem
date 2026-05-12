using System.Collections.Generic;

namespace TechCosmos.SkillSystem.Runtime
{
    public class ConditionLayer<T> : IConditionLayer<T> where T : class, IUnit<T>
    {
        public List<Condition<T>> Conditions { get; set; }
        public ISkill<T> Skill { get; set; }

        public bool CheckCondition(SkillContext<T> skillContext)
        {
            if (Conditions == null || Conditions.Count == 0)
                return true;

            for (int i = 0; i < Conditions.Count; i++)
            {
                if (Conditions[i] != null && !Conditions[i].IsEligible(skillContext, Skill.DataLayer))
                    return false;
            }
            return true;
        }

        public ConditionLayer(List<Condition<T>> conditions = null)
        {
            Conditions = conditions ?? new List<Condition<T>>();
        }
    }
}